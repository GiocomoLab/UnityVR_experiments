using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController_JitterCues1 : MonoBehaviour
{
    public bool saveData = true;

    public ObjectSetHandler objectSetHandler;
    public ArduinoHandler arduinoHandler;

    public int numTraversals = 0;
    public int numTraversalsDarkBefore = 0;
    public int numTraversalsDarkAfter = 0;

    public float trackLenA = 450.0f;
    public float trackLenB = 400.0f;
    public float trackOffsetA = 0.0f;
    public float trackOffsetB = 200.0f;

    //public int environmentPair = 1;

    public float startOffsetMin = 0.0f;
    public float startOffsetMax = 0.0f; //75.0f; Test this later

    public bool UseTimeout = true;
    public float timeoutMin = 2.0f;
    public float timeoutMax = 4.0f;
    private bool haveSetTimeoutEnd = false;
    private float timeoutOver = 0.0f;

    public List<int> trialNumbersA;
    public List<bool> blocksStableA;

    public List<int> trialNumbersB;
    public List<bool> blocksStableB;

    private List<int> trialContext; 
    private List<bool> trialStable; 
    private List<int> trialNum; 
    private List<int> trialBlockNums;
    

    [HideInInspector] public float[] trackOffsets = new float[2];
    [HideInInspector] public float[] trackLens = new float[2];
    private float trackOffset_x;

    public int lickPinValue;
    public int lickThresh = 500;
    public int lickFlag;
    private int numLicks;
    private int rewardFlag;

    public GameObject blackoutEnd;
    public float darkTrackOffset_x = -200.0f;
    public float darkTrackLength = 400.0f;
    public int numDarkLapsBefore = 30;
    public int numDarkLapsAfter = 30;
    private int numTotalLaps;

    private bool recordingStarted = false;
    private bool sentStartSignal = false;
    private int cmdWrite;

    private int rotaryTicks, syncPinState, startStopPinState, missedArdFrame;

    private float speed = 0.0447f;
    private float delta_z, current_z, delta_T;

    private bool endSessionE, endSessionU, dispEndSessE, dispEndSessU, ifSetSkipToEnd;
    private bool justTeleported, allowMovement;

    private Vector3 lastPosition;

    private Vector3 blackoutBoxPosition = new Vector3(0.0f, -10.0f, 600.0f);

    void Awake()
    {
        Debug.Log("player controller wake");
        //Probably going to handle some stuff here so don't get rid of it or move things forward in other scripts

        trackOffsets[0] = trackOffsetA; trackOffsets[1] = trackOffsetB;
        trackLens[0] = trackLenA; trackLens[1] = trackLenB;
    }
    
    void Start()
    {
        Debug.Log("PlayerController start");

        trialContext = objectSetHandler.trialContext;
        trialStable = objectSetHandler.trialStable;
        trialNum = objectSetHandler.trialNum;
        trialBlockNums = objectSetHandler.trialBlockNums;
        numTotalLaps = trialContext.Count;

        float initialPosition_z = 0.0f;
        if (startOffsetMax > 0.0f) { initialPosition_z = 0.0f - Random.Range(startOffsetMin, startOffsetMax); }
        Debug.Log(initialPosition_z);
        trackOffset_x = trackOffsets[trialContext[0] - 1];
        Vector3 initialPosition = new Vector3(trackOffset_x, 0.5f, initialPosition_z);
        transform.position = initialPosition;

        //objectSetHandler.SetTrialObjectPositions(0); // Handled by object set handler
    }

    // Update is called once per frame
    void Update()
    {
        justTeleported = false; 

        cmdWrite = 1;
        if (recordingStarted == false)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                Debug.Log("starting Update");
                //Debug.Log("numTraversals = " + numTraversals + " / numTrialsTotal = " + numTrialsTotal);
                //Debug.Log("Trial " + (numTraversals + 1) + ", reward position = " + rewardDist);
                recordingStarted = true;
            }

            cmdWrite = 1;
            arduinoHandler.sendCmdReadReply(cmdWrite);
            lickPinValue = arduinoHandler.lickPinValue;

            //if (lickPinValue > 500 & lickFlag == 1)
            if (lickPinValue > lickThresh)
            {
                lickFlag = 0;
            }
            //if (lickPinValue < 500 & lickFlag == 0)
            if (lickPinValue <= lickThresh)
            {
                numLicks += 1;
                lickFlag = 1;
            }
        }

        if (recordingStarted == true)
        {

            if (sentStartSignal == false)
            {
                arduinoHandler.sendStartSignal();

                sentStartSignal = true;
                //if (saveData == true)
                //{
                //    sw_startstop.Write("StartSignal" + "\t" + Time.realtimeSinceStartup + "\n");
                //}
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

            if (numTraversalsDarkBefore < numDarkLapsBefore)
            {
                lastPosition.x = darkTrackOffset_x;

                if (lastPosition.z >= darkTrackLength)
                {
                    lastPosition.z = 0.0f;
                    numTraversalsDarkBefore++;
                    Debug.Log("Done post dark lap " + numTraversalsDarkAfter + " / " + numDarkLapsAfter);
                }

                transform.position = lastPosition;
                Vector3 blackoutPos = blackoutEnd.transform.position;
                blackoutPos.z = lastPosition.z + 15.0f;
                blackoutEnd.transform.position = blackoutPos;
            }

            if (numTraversalsDarkBefore >= numDarkLapsBefore)
            {
                if (numTraversals < numTotalLaps)
                {
                    lastPosition.x = trackOffsets[trialContext[numTraversals] - 1];

                    // Evaluate reward

                    // End of lap stuff

                    // teleport
                    if (transform.position.z > trackLens[trialContext[numTraversals]-1]) // | (transform.position.z < timeoutLimit)
                    {
                        bool allowLapEnd = true;

                        if (endSessionU == true)
                        {
                            if (ifSetSkipToEnd == false)
                            {
                                Debug.Log("Recieved keypress sequence E - U, skipping to end of session");
                                numTraversals = numTotalLaps - 1;
                                ifSetSkipToEnd = true;
                            }
                        }

                        if (UseTimeout == true)
                        {
                            if (numTraversals+1 == numTotalLaps)
                            {
                                haveSetTimeoutEnd = true;
                                timeoutOver = Time.realtimeSinceStartup - 1.0f;
                            }

                            if (haveSetTimeoutEnd == false)
                            {
                                float timeoutDelay = UnityEngine.Random.Range(timeoutMin, timeoutMax);
                                if (timeoutMax <= 0.0f) { timeoutDelay = -1.0f; }
                                
                                
                                timeoutOver = Time.realtimeSinceStartup + timeoutDelay;
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
                            else if (timeNow > timeoutOver)
                            {
                                allowLapEnd = true;
                                haveSetTimeoutEnd = false; // Allows it to be reset next lap
                                allowMovement = true;
                            }
                        }

                        if (allowLapEnd == true)
                        {
                            numTraversals += 1;
                            Debug.Log("Ending lap: numTraversals = " + numTraversals + " / numTrialsTotal = " + numTotalLaps);
                            
                            if (numTraversals < numTotalLaps) // Don't teleport on last sample
                            {
                                transform.position = new Vector3(trackOffsets[trialContext[numTraversals] - 1], lastPosition[1], 0.0f);
                                lastPosition = transform.position;
                                justTeleported = true;
                            }

                            rewardFlag = 0;

                            if (saveData)
                            {
                                //sw_trial.Write(Time.realtimeSinceStartup + "\t" + numTraversals + "\n");
                            }

                            if (numTraversals < numTotalLaps)
                            {

                                if (numTraversals == (numTotalLaps - 1))
                                {
                                    Debug.Log("this was the second to last lap");
                                }
                            }
                            else
                            {
                                Debug.Log("This was the last lap, numTraversals = " + numTraversals);
                                numTraversals = numTotalLaps; // for easier indexing
                            }

                        }
                    }
                }
            }

            if (numTraversals >= numTotalLaps)
            {
                if (numTraversalsDarkAfter < numDarkLapsAfter)
                {
                    lastPosition.x = darkTrackOffset_x;

                    if (lastPosition.z >= darkTrackLength)
                    {
                        lastPosition.z = 0.0f;
                        numTraversalsDarkAfter++;
                        Debug.Log("Done post dark lap " + numTraversalsDarkAfter + " / " + numDarkLapsAfter);
                    }
                }

                if (numTraversalsDarkAfter >= numDarkLapsAfter)
                {
                    //End session
                    Debug.Log("Stopping session at " + Time.realtimeSinceStartup + "\n");
                    //sw_startstop.Write("StopSignalPlanned" + "\t" + Time.realtimeSinceStartup + "\n");
                    UnityEditor.EditorApplication.isPlaying = false;
                }

                transform.position = lastPosition;
                Vector3 blackoutPos = blackoutEnd.transform.position;
                blackoutPos.z = lastPosition.z + 15.0f;
                blackoutEnd.transform.position = blackoutPos;
            }

            // Update position, etc.
            missedArdFrame = 0;
            arduinoHandler.sendCmdReadReply(cmdWrite);
            lickPinValue = arduinoHandler.lickPinValue;
            rotaryTicks = arduinoHandler.rotaryTicks;
            syncPinState = arduinoHandler.syncPinState;
            startStopPinState = arduinoHandler.startStopPinState;
            missedArdFrame = arduinoHandler.missedArdFrame;

            if ( lickPinValue < lickThresh ) { lickFlag = 1; }
            if (lickFlag==1) { numLicks += 1; }

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
                    if (new_z < -5.0f)
                    {
                        new_z = 0.0f;
                    }
                    transform.position = new Vector3(lastPosition[0], lastPosition[1], new_z);
                }
                lastPosition = transform.position;
            }

        }
    }

    // save trial data to server
    void OnApplicationQuit()
    {
        Debug.Log("Quitting");
        cmdWrite = 4;
        arduinoHandler.sendCmdReadReply(cmdWrite);

        /*
        if (saveData == true)
        {
            sw_startstop.Write("StopSignalSent" + "\t" + Time.realtimeSinceStartup + "\n");

            sw_startstop.Close();
            sw_trial.Close();
            sw_pos.Close();
            sw_reward.Close();
            sw_lick.Close();
            sw_trialList.Close();
            sw_trialListGM.Close();
            sw_par.Close();
            sw_parpar.Close();

            try
            {
                File.Copy(trialTimesFile, serverTrialTimesFile);
                Debug.Log("Copied the trialTimes file");
                File.Copy(rewardFile, serverRewardFile);
                Debug.Log("Copied the reward file");
                File.Copy(positionFile, serverPositionFile);
                Debug.Log("Copied the position file");
                File.Copy(lickFile, serverLickFile);
                Debug.Log("Copied the lick file");
                File.Copy(startStopFile, serverStartStopFile);
                Debug.Log("Copied the startStop file");
                File.Copy(blockInfoFile, serverBlockInfoFile);
                Debug.Log("Copied the blockInfo file");
                File.Copy(paramsTwoFile, serverParamsTwoFile);
                Debug.Log("Copied the paramsTwo file");
                File.Copy(trialListFile, serverTrialListFile);
                Debug.Log("Copied the trialList file");
                File.Copy(trialListFileGM, serverTrialListFileGM);
                Debug.Log("Copied the trialListGM file");
            }
            catch
            {

            }
        }
        */
    }
}
