using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController_JitterCues1 : MonoBehaviour
{
    // This is the basic player controller command
    public ObjectSetHandler objectSetHandler;
    public ArduinoHandler arduinoHandler;

    public int numTraversals = 0;

    public float trackLenA = 450.0f;
    public float trackLenB = 400.0f;
    public float trackOffsetA = 0.0f;
    public float trackOffsetB = 200.0f;

    //public int environmentPair = 1;

    public float startOffsetMin = 0.0f;
    public float startOffsetMax = 0.0f; //75.0f; Test this later

    public float timeoutMin = 2.0f;
    public float timeoutMax = 4.0f;

    public List<int> trialNumbersA;
    public List<bool> blocksStableA;

    public List<int> trialNumbersB;
    public List<bool> blocksStableB;

    private List<int> trialContext; 
    private List<bool> trialStable; 
    private List<int> trialNum; 
    private List<int> trialBlockNums;

    [HideInInspector] public float[] trackOffsets = new float[2];
    private float trackOffset_x;

    public int lickPinValue;
    public int lickThresh = 500;
    public int lickFlag;
    private int numLicks;

    private bool recordingStarted = false;
    private bool sentStartSignal = false;
    private int cmdWrite;

    private bool endSessionE, endSessionU, dispEndSessE, dispEndSessU;

    void Awake()
    {
        Debug.Log("player controller wake");
        //Probably going to handle some stuff here so don't get rid of it or move things forward in other scripts

        trackOffsets[0] = trackOffsetA; trackOffsets[1] = trackOffsetB;
    }
    
    void Start()
    {
        Debug.Log("PlayerController start");

        trialContext = objectSetHandler.trialContext;
        trialStable = objectSetHandler.trialStable;
        trialNum = objectSetHandler.trialNum;
        trialBlockNums = objectSetHandler.trialBlockNums;

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

            // Rest of normal update
            //check for reward

            // Check end lap stuff

            // Update position, etc.


        }
    }
}
