using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class PlayerController_JitterCues1 : MonoBehaviour
{
    public bool saveData = true;
    private int target = 30;

    public ObjectSetHandler objectSetHandler;
    public ArduinoHandler arduinoHandler;
    public SessionParams sessionParams;

    public int numTraversals = 0;
    private int numTraversalsThisBlock = 0;
    private int numTraversalsDarkBefore = 0;
    private int numTraversalsDarkAfter = 0;
    private int thisBlockNum, lastBlockNum;

    public int lickPinValue;
    public int lickThresh = 500;
    public int lickFlag;

    public float trackLenA = 450.0f;
    public float trackLenB = 400.0f;
    public float trackOffsetA = 0.0f;
    public float trackOffsetB = 200.0f;
    public float rewardZoneA = 0.75f;
    public float rewardZoneB = 0.5f;
    public float rewardZoneSizeTotal = 25.0f;
    private float rewardLocation, rewardZoneStart, rewardZoneEnd;
    [HideInInspector] private float[] rewardZones = new float[2];

    public float startOffsetMin = 0.0f;
    public float startOffsetMax = 0.0f; //75.0f; Test this later

    public bool UseTimeout = true;
    public float timeoutMin = 2.0f;
    public float timeoutMax = 4.0f;
    private bool haveSetTimeoutEnd = false;
    private float timeoutOver = 0.0f;

    public List<int> trialNumbersA, trialNumbersB;
    public List<bool> blocksStableA, blocksStableB;
    public List<int> rewardsAtStartA, rewardsAtStartB;
    public List<int> missesBeforeReminderA, missesBeforeReminderB;
    private int[] rewardsAtStart, missesBeforeReminder;

    private List<bool> trialStable;
    private List<int> trialNum, trialBlockNums, trialContext;
    private List<float[]> trialObjectPositions;
    private List<int[]> trialObjectNums;

    [HideInInspector] public float[] trackOffsets = new float[2];
    [HideInInspector] public float[] trackLens = new float[2];
    private float trackOffset_x;
    private float trackLenNow;

    private int rewardFlag;
    private int requestedRewards, freeRewards, missedRewards, automaticReward, numLicks, numMissedRewards;
    private int anticipatoryLicks, rewardLicks, consummatoryLicks, innaccurateLicks;
    private float anticipatoryLickRate, rewardLickRate, consummatoryLickRate, probablyGoodLickRate;

    public GameObject blackoutEnd;
    public float darkTrackOffset_x = -200.0f;
    public float darkTrackLength = 400.0f;
    public int numDarkLapsBefore = 30;
    public int numDarkLapsAfter = 30;
    private int numRegularLaps;

    private bool recordingStarted = false;
    private bool sentStartSignal = false;
    private int cmdWrite;

    private int rotaryTicks, syncPinState, startStopPinState, missedArdFrame;
    private int pulseMarker = 1;

    private float speed = 0.0447f;
    private float delta_z, current_z, delta_T;

    private bool endSessionE, endSessionU, dispEndSessE, dispEndSessU, ifSetSkipToEnd, allowEndSession;
    private bool justTeleported, allowMovement;
    private bool dispRegLapsStart, dispDarkBeforeLapsStart, dispDarkAfterLapsStart;
    private bool doneDarkLapsBefore, doneRegularLaps;

    private Vector3 lastPosition;
    private Vector3 blackoutBoxPosition = new Vector3(0.0f, -10.0f, 600.0f);

    private string fullLocalStr, fullServerStr, mouse, session, trackName;
    private string trialTimesFile, serverTrialTimesFile, rewardFile, serverRewardFile, lickFile, serverLickFile, positionFile, serverPositionFile, startStopFile, serverStartStopFile, paramsTwoFile, serverParamsTwoFile,trialListFile,serverTrialListFile;
    private string positionHeaderFile, serverPositionHeaderFile;
    private StreamWriter sw_lick, sw_pos, sw_trial, sw_reward, sw_startstop, sw_par, sw_trialList, sw_trialOrder, sw_pos_header;
    private bool wrotePosHeader = false;

    void Awake()
    {
        Debug.Log("player controller wake");
        //Probably going to handle some stuff here so don't get rid of it or move things forward in other scripts

        trackOffsets[0] = trackOffsetA; trackOffsets[1] = trackOffsetB;
        trackLens[0] = trackLenA; trackLens[1] = trackLenB;
        rewardZones[0] = rewardZoneA; rewardZones[1] = rewardZoneB;
    }
    
    void Start()
    {
        Debug.Log("PlayerController start; HAS TO RUN BEFORE OBJECTHANDLER!");

        if (timeoutMax <= 0.0f) { UseTimeout = false; }

        trialContext = objectSetHandler.trialContext;
        trialStable = objectSetHandler.trialStable;
        trialNum = objectSetHandler.trialNum;
        trialBlockNums = objectSetHandler.trialBlockNums;
        trialObjectNums = objectSetHandler.trialObjectNums;
        trialObjectPositions = objectSetHandler.trialObjectPositions;
        numRegularLaps = trialContext.Count;

        float initialPosition_z = 0.0f;
        if (startOffsetMax > 0.0f) { initialPosition_z = 0.0f - Random.Range(startOffsetMin, startOffsetMax); }
        Debug.Log(initialPosition_z);
        trackOffset_x = trackOffsets[trialContext[0] - 1];
        Vector3 initialPosition = new Vector3(darkTrackOffset_x, 0.5f, 0.0f);
        transform.position = initialPosition;

        if (numDarkLapsBefore == 0)
        {
            doneDarkLapsBefore = true;
        }


        missesBeforeReminder = new int[missesBeforeReminderA.Count + missesBeforeReminderB.Count];
        rewardsAtStart = new int[rewardsAtStartA.Count + rewardsAtStartB.Count];
        for (int ii=0; ii<Mathf.Max(missesBeforeReminderA.Count, missesBeforeReminderB.Count); ii++)
        {
            if (ii < missesBeforeReminderA.Count)
            {
                missesBeforeReminder[(ii+1) * 2 - 2] = missesBeforeReminderA[ii];
            }
            if (ii < missesBeforeReminderB.Count)
            {
                missesBeforeReminder[(ii+1) * 2 - 1] = missesBeforeReminderB[ii];
            }

            if (ii < rewardsAtStartA.Count)
            {
                rewardsAtStart[(ii+1) * 2 - 2] = rewardsAtStartA[ii];
            }
            if (ii < rewardsAtStartB.Count)
            {
                rewardsAtStart[(ii+1) * 2 - 1] = rewardsAtStartB[ii];
            }
        }

        fullLocalStr = sessionParams.fullLocalStr;
        fullServerStr = sessionParams.fullServerStr;
        mouse = sessionParams.mouse;
        session = sessionParams.session;
        trackName = sessionParams.trackName;

        if (saveData == true)
        {
            trialTimesFile = fullLocalStr + "_trial_times.txt"; serverTrialTimesFile = fullServerStr + "_trial_times.txt";
            rewardFile = fullLocalStr + "_reward.txt"; serverRewardFile = fullServerStr + "_reward.txt";
            positionFile = fullLocalStr + "_position.txt"; serverPositionFile = fullServerStr + "_position.txt";
            positionHeaderFile = fullLocalStr + "_position_header.txt"; serverPositionHeaderFile = fullServerStr + "_position_header.txt";
            lickFile = fullLocalStr + "_licks.txt"; serverLickFile = fullServerStr + "_licks.txt";
            startStopFile = fullLocalStr + "_startStop.txt"; serverStartStopFile = fullServerStr + "_startStop.txt";
            paramsTwoFile = fullLocalStr + "_params2.txt"; serverParamsTwoFile = fullServerStr + "_params2.txt";
            trialListFile = fullLocalStr + "_trialList.txt"; serverTrialListFile = fullServerStr + "_trialList.txt";

            sw_lick = new StreamWriter(lickFile, true);
            sw_pos = new StreamWriter(positionFile, true);
            sw_trial = new StreamWriter(trialTimesFile, true);
            sw_reward = new StreamWriter(rewardFile, true);
            sw_startstop = new StreamWriter(startStopFile, true);
            sw_par = new StreamWriter(paramsTwoFile, true);
            sw_trialList = new StreamWriter(trialListFile, true);
            sw_pos_header = new StreamWriter(positionHeaderFile, true);

            // Save some params
            sw_par.Write("mouse" + "\t" + mouse + "\n");
            sw_par.Write("session" + "\t" + session + "\n");
            sw_par.Write("trackName" + "\t" + trackName + "\n");
            sw_par.Write("rewardZoneSizeTotal" + "\t" + rewardZoneSizeTotal + "\n");
            sw_par.Write("blackoutBoxPosition" + "\t" + blackoutBoxPosition + "\n");
            sw_par.Write("UseTimeout" + "\t" + UseTimeout + "\n");
            sw_par.Write("TimeoutMinimum" + "\t" + timeoutMin + "\n");
            sw_par.Write("TimeoutMaximum" + "\t" + timeoutMax + "\n");
            sw_par.Write("numDarkLapsBefore" + "\t" + numDarkLapsBefore + "\n");
            sw_par.Write("numLapsMain" + "\t" + trialContext.Count + "\n");
            sw_par.Write("numDarkLapsBefore" + "\t" + numDarkLapsAfter + "\n");

            SaveTrialListToFile();
        }

        if (Application.targetFrameRate != target)
            Application.targetFrameRate = target;

        lastBlockNum = -1;
        cmdWrite = 1;
    }

    // Update is called once per frame
    void Update()
    {
        if (Application.targetFrameRate != target)
            Application.targetFrameRate = target;

        justTeleported = false; 

        if (recordingStarted == false)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                Debug.Log("starting Update");
                recordingStarted = true;
            }

            cmdWrite = 1;
            arduinoHandler.sendCmdReadReply(cmdWrite);
            lickPinValue = arduinoHandler.lickPinValue;

            if (lickPinValue > lickThresh)
            {
                lickFlag = 0;
            }
            if (lickPinValue <= lickThresh)
            {
                numLicks++;
                lickFlag = 1;
            }
        }

        if (recordingStarted == true)
        {
            if (sentStartSignal == false)
            {
                arduinoHandler.sendStartSignal();

                sentStartSignal = true;
                if (saveData == true) { sw_startstop.Write("StartSignal" + "\t" + Time.realtimeSinceStartup + "\n"); }
            }

            // Press e, then u, to skip to black timeout
            if (Input.GetKeyDown(KeyCode.E))
            {
                endSessionE = true;
                if (dispEndSessE == false)
                {
                    Debug.Log("Now press U to skip to blackbox running");
                }
            }

            if (Input.GetKeyDown(KeyCode.U))
            {
                endSessionU = true;
                if (endSessionE == true)
                {
                    if (dispEndSessU == false)
                    {
                        Debug.Log("Skipping to blackbox running");
                        dispEndSessU = true;
                    }
                }
            }

            // Update position, etc.
            CallArduinoUpdateMovement();
            cmdWrite = 1;

            if (numTraversalsDarkBefore < numDarkLapsBefore && doneDarkLapsBefore == false)
            {
                if (dispDarkBeforeLapsStart == false) { Debug.Log("Starting early dark laps"); dispDarkBeforeLapsStart = true; }
                allowMovement = true;

                lastPosition.x = darkTrackOffset_x;

                if (lastPosition.z >= darkTrackLength)
                {
                    lastPosition.z = 0.0f;
                    numTraversalsDarkBefore++;
                    if (numTraversalsDarkBefore >= numDarkLapsBefore) { doneDarkLapsBefore = true; }
                    Debug.Log("Done post dark lap " + numTraversalsDarkBefore + " / " + numDarkLapsBefore);
                }

                transform.position = lastPosition;
            }

            if (doneDarkLapsBefore == true && doneRegularLaps == false) //(numTraversalsDarkBefore >= numDarkLapsBefore)
            {
                if (dispRegLapsStart == false) { Debug.Log("Starting regular laps"); dispRegLapsStart = true; }
                allowMovement = true;
                lastPosition.x = trackOffsets[trialContext[numTraversals] - 1];
                trackLenNow = trackLens[trialContext[numTraversals] - 1];

                thisBlockNum = trialBlockNums[numTraversals];
                if (thisBlockNum != lastBlockNum)
                {
                    numMissedRewards = 0;
                    numTraversalsThisBlock = 0;
                    lastBlockNum = thisBlockNum;
                }

                // Evaluate reward
                rewardLocation = rewardZones[trialContext[numTraversals] - 1]*trackLenNow;
                rewardZoneStart = rewardLocation - rewardZoneSizeTotal / 2.0f;
                rewardZoneEnd = rewardLocation + rewardZoneSizeTotal / 2.0f;
                UpdateLickingAccuracy();

                automaticReward = 0;
                if ((numTraversalsThisBlock < rewardsAtStart[thisBlockNum]) || (numMissedRewards >= missesBeforeReminder[thisBlockNum]))
                {
                    automaticReward = 1;
                }

                if (transform.position.z >= rewardZoneStart && rewardFlag == 0)
                {
                    if (transform.position.z <= rewardZoneEnd)
                    {
                        if (lickFlag == 1)
                        {
                            cmdWrite = 2;
                            rewardFlag = 1;
                            // All the other reward stuff
                            Debug.Log("Requested reward trial " + (numTraversals + 1));
                            requestedRewards++;
                            numMissedRewards = 0;
                        }

                        if (transform.position.z >= rewardLocation && automaticReward==1)
                        {
                            cmdWrite = 2;
                            rewardFlag = 1;
                            Debug.Log("Automatic reward trial " + (numTraversals + 1));
                            freeRewards++;
                            numMissedRewards = 0;
                        }
                    }

                    if (transform.position.z > rewardZoneEnd && rewardFlag == 0)
                    {
                        Debug.Log("Missed reward trial " + (numTraversals + 1));
                        missedRewards++;
                        numMissedRewards++;
                        rewardFlag = 1;
                    }
                }

                // teleport/end of lap
                if (transform.position.z > trackLenNow) 
                {
                    bool allowLapEnd = true;

                    lastPosition.z = trackLenNow + 0.1f;
                    transform.position = lastPosition;

                    if (endSessionU == true)
                    {
                        if (ifSetSkipToEnd == false)
                        {
                            Debug.Log("Recieved keypress sequence E - U, skipping to end of session");
                            numTraversals = numRegularLaps - 1;
                            ifSetSkipToEnd = true;
                        }
                    }

                    if (UseTimeout == true)
                    {
                        if (haveSetTimeoutEnd == false)
                        {
                            float timeoutDelay = UnityEngine.Random.Range(timeoutMin, timeoutMax);
                            timeoutOver = Time.realtimeSinceStartup + timeoutDelay;
                            if (numTraversals + 1 == numRegularLaps) { timeoutOver = Time.realtimeSinceStartup - 1.0f; } // Don't run a delay on the last lap, it'll get confusing with dark running
                            haveSetTimeoutEnd = true;
                            Debug.Log("Time out for " + timeoutDelay + "s" + "; This was at " + System.DateTime.Now);
                        }

                        float timeNow = Time.realtimeSinceStartup;
                        allowLapEnd = false;
                        if (timeNow <= timeoutOver)
                        {
                            transform.position = blackoutBoxPosition;
                            allowMovement = false;
                        }
                        else //(timeNow > timeoutOver)
                        {
                            allowLapEnd = true;
                            haveSetTimeoutEnd = false; // Allows it to be reset for next lap
                            allowMovement = true;
                        }
                    }

                    if (allowLapEnd == true)
                    {
                        numTraversals++;
                        numTraversalsThisBlock++;
                        rewardFlag = 0;
                        Debug.Log("Ending lap: numTraversals = " + numTraversals + " / numTrialsTotal = " + numRegularLaps);
                        Debug.Log("Reward summary: requested " + requestedRewards + ", automatic " + freeRewards + ", out of nlaps " + numTraversals);

                        if (numTraversals == (numRegularLaps - 1)) { Debug.Log("this was the second to last lap"); }

                        if (numTraversals < numRegularLaps) // Don't teleport on last sample
                        {
                            transform.position = new Vector3(trackOffsets[trialContext[numTraversals] - 1], 0.5f, 0.0f);
                            lastPosition = transform.position;
                            justTeleported = true;
                        }

                        if (numTraversals == numRegularLaps)
                        {
                            Debug.Log("This was the last lap, numTraversals = " + numTraversals);
                            doneRegularLaps = true;
                            numTraversals = numTraversals - 1; // for easier indexing, will go over on last frame of last lap
                        }

                        if (saveData)
                        {
                            sw_trial.Write(Time.realtimeSinceStartup + "\t" + numTraversals + "\t" + Time.frameCount + "\n");
                        }

                        DisplayLickingAccuracy();
                    }
                }
                if ((transform.position.z > trackLenNow) && (transform.position.z < blackoutBoxPosition.z))
                {
                    Debug.Log("beyond track end but not in box, numTraversals " + numTraversals);
                }
            }

            if (doneRegularLaps == true) //numTraversals >= numRegularLaps && 
            {
                if (numTraversalsDarkAfter < numDarkLapsAfter)
                {
                    if (dispDarkAfterLapsStart == false) { Debug.Log("Starting late dark laps"); dispDarkAfterLapsStart = true; }

                    allowMovement = true;
                    lastPosition.x = darkTrackOffset_x;

                    if (lastPosition.z >= darkTrackLength)
                    {
                        numTraversalsDarkAfter++;
                        if (numTraversalsDarkAfter >= numDarkLapsAfter)
                        {
                            //End session
                            numTraversalsDarkAfter = numTraversalsDarkAfter - 1;
                            allowEndSession = true;
                        }
                        else
                        {
                            lastPosition.z = 0.0f;
                        }
                        Debug.Log("Done post dark lap " + numTraversalsDarkAfter + " / " + numDarkLapsAfter);
                    }
                }
                transform.position = lastPosition;
            }

            // For dark running
            Vector3 blackoutPos = blackoutEnd.transform.position;
            blackoutPos.z = lastPosition.z + 15.0f;
            blackoutEnd.transform.position = blackoutPos;

            SaveUpdateData();

            if (allowEndSession == true)
            {
                Debug.Log("Stopping session at " + Time.realtimeSinceStartup + "\n");
                sw_startstop.Write("StopSignalPlanned" + "\t" + Time.realtimeSinceStartup + "\n");
                UnityEditor.EditorApplication.isPlaying = false;
            }

            pulseMarker = pulseMarker * -1;
        } // recording started true
    } // update

    void CallArduinoUpdateMovement()
    {
        missedArdFrame = 0;
        //Debug.Log("cmdWrite " + cmdWrite);
        arduinoHandler.sendCmdReadReply(cmdWrite);
        lickPinValue = arduinoHandler.lickPinValue;
        rotaryTicks = arduinoHandler.rotaryTicks;
        syncPinState = arduinoHandler.syncPinState;
        startStopPinState = arduinoHandler.startStopPinState;
        missedArdFrame = arduinoHandler.missedArdFrame;
        //Debug.Log("rotatry Ticks " + rotaryTicks);
        lickFlag = 0;
        if (lickPinValue < lickThresh) { lickFlag = 1; numLicks++; }

        delta_T = Time.deltaTime;
        delta_z = rotaryTicks * speed;

        if (justTeleported == true)
        {
            rotaryTicks = 0;
            delta_z = 0f;
        }

        current_z = transform.position.z;
        if (allowMovement == true)
        {
            if (rotaryTicks == 0)
            {
                transform.position = lastPosition; // possible could get stuck here on a lap transition
            }
            else
            {
                float new_z = current_z + delta_z;
                if (new_z < -5.0f) { new_z = 0.0f; }
                transform.position = new Vector3(lastPosition[0], lastPosition[1], new_z);
            }
            lastPosition = transform.position;
        }
    }

    void SaveUpdateData() 
    {
        if (saveData == true)
        {
            try
            {
                int numTraversalsWrite = numTraversals;
                if (doneDarkLapsBefore == false) { numTraversalsWrite = numTraversalsDarkBefore - numDarkLapsBefore; }
                if (doneRegularLaps == true) { numTraversalsWrite = numTraversalsDarkAfter + (numRegularLaps-1); };
                sw_pos.Write(transform.position.z + "\t" + Time.realtimeSinceStartup + "\t" + syncPinState + "\t" +
                                pulseMarker + "\t" + startStopPinState + "\t" + delta_z + "\t" + lickPinValue + "\t" +
                                rewardFlag + "\t" + numTraversalsWrite + "\t" + delta_T + "\t" + missedArdFrame + "\n");

                if (wrotePosHeader == false)
                {
                    sw_pos_header.Write("transform.position.z" + "\t" + "Time.realtimeSinceStartup" + "\t" + "syncPinState" + "\t" +
                                "pulseMarker" + "\t" + "startStopPinState" + "\t" + "delta_z" + "\t" + "lickPinValue" + "\t" +
                                "rewardFlag" + "\t" + "numTraversals" + "\t" + "delta_T" + "\t" + "missedArdFrame" + "\n");
                    wrotePosHeader = true;
                }
            }
            catch
            {
                Debug.Log("failed to write save file frame " + Time.frameCount);
            }
        }
    }

    void UpdateLickingAccuracy()
    {
        if (lickFlag == 1)
        {
            if ((transform.position.z < rewardZoneStart) && (transform.position.z > (rewardZoneStart - 50.0f))) // Anticipatory licking
            {
                anticipatoryLicks++; 
            }
            else if ((transform.position.z > rewardZoneStart) && (transform.position.z < rewardZoneEnd)) // Reward zone licking
            {
                rewardLicks++; 
            }
            else if ((transform.position.z > rewardZoneEnd) && (transform.position.z < (rewardZoneEnd+50.0f)) && (rewardFlag == 1)) // Probably consummatory
            {
                consummatoryLicks++; 
            }
            else // Innaccurate licking
            {
                innaccurateLicks++; 
            }

            anticipatoryLickRate = (float)anticipatoryLicks / (float)numLicks;
            rewardLickRate = (float)rewardLicks / (float)numLicks;
            consummatoryLickRate = (float)consummatoryLicks / (float)numLicks;
            probablyGoodLickRate = anticipatoryLickRate + rewardLickRate + consummatoryLickRate;
        }
    }

    void DisplayLickingAccuracy()
    {
        //Debug.Log("anticipatoryLicks " + anticipatoryLicks + " rewardLicks " + rewardLicks + " consummatoryLicks " + consummatoryLicks + " numLicks " + numLicks);
        Debug.Log("anticipatoryLickRate " + anticipatoryLickRate + " rewardLickRate " + rewardLickRate + " consummatoryLickRate " + consummatoryLickRate);
        //Debug.Log("On trial " + (numTraversals + 1) + " probablyGoodLickRate = " + probablyGoodLickRate);
    }

    void SaveTrialListToFile()
    {
        Debug.Log("Writing trial list to file");
        sw_trialList.Write("trialI" + "\t" + "trialStable" + "\t" + "trialContext" + "\t" + "obj1" + "\t" + "obj2" + "\t" + "obj3" + "\t" + "obj4" + "\t" + "obj5" + "\t" + "pos1" + "\t" + "pos2" + "\t" + "pos3" + "\t" + "pos4" + "\t" + "pos5" +"\n");
        for (int trialI = 0; trialI < trialNum.Count; trialI++)
        {
            int ts = 0; if (trialStable[trialI] == true) { ts = 1; }
            string writeStr = trialI + "\t" + ts + "\t" + trialContext[trialI] + "\t";
            int[] objectsH = trialObjectNums[trialI];
            float[] positionsH = trialObjectPositions[trialI];
            for (int objI = 0; objI < 5; objI++)
            {
                int objNumH = -1;
                if (objectsH.Length - 1 >= objI)
                {
                    objNumH = objectsH[objI];
                }
                writeStr = writeStr + "\t" + objNumH;
            }
            for (int objI = 0; objI < 5; objI++)
            {
                float objPosH = -0.5f;
                if (objectsH.Length - 1 >= objI)
                {
                    objPosH = positionsH[objI];
                }
                writeStr = writeStr + "\t" + objPosH;
            }
            sw_trialList.Write(writeStr + "\n");
        }
    }

    // save trial data to server
    void OnApplicationQuit()
    {
        Debug.Log("Quitting");
        cmdWrite = 4;
        arduinoHandler.sendCmdReadReply(cmdWrite);

        if (saveData == true)
        {
            sw_startstop.Write("StopSignalSent" + "\t" + Time.realtimeSinceStartup + "\n");

            sw_startstop.Close();
            sw_trial.Close();
            sw_pos.Close();
            sw_reward.Close();
            sw_lick.Close();
            sw_trialList.Close();
            sw_par.Close();
            sw_pos_header.Close();

            try
            {
                File.Copy(trialTimesFile, serverTrialTimesFile);
                Debug.Log("Copied the trialTimes file");
                File.Copy(rewardFile, serverRewardFile);
                Debug.Log("Copied the reward file");
                File.Copy(positionFile, serverPositionFile);
                Debug.Log("Copied the position file");
                File.Copy(positionHeaderFile, serverPositionHeaderFile);
                Debug.Log("Copied the position header file");
                File.Copy(lickFile, serverLickFile);
                Debug.Log("Copied the lick file");
                File.Copy(startStopFile, serverStartStopFile);
                Debug.Log("Copied the startStop file");
                File.Copy(paramsTwoFile, serverParamsTwoFile);
                Debug.Log("Copied the paramsTwo file");
                File.Copy(trialListFile, serverTrialListFile);
                Debug.Log("Copied the trialList file");
            }
            catch
            {

            }
        }
    }
}
