using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectSetHandler : MonoBehaviour
{
    public PlayerController_JitterCues1 playerController;

    public List<GameObject> allObjectCues;

    public int[] objectSetA = { 0, 1, 2, 3, 4 };
    public float[] positionsRelA = { 0.1f, 0.21f, 0.45f, 0.6f, 0.87f };
    public int[] objectSetB = { 5, 6, 7, 8 };
    public float[] positionsRelB = { 0.15f, 0.3f, 0.55f, 0.9f };

    public float[] jitterRangesA = {0.1f, 0.1f, 0.1f, 0.1f, 0.1f};
    public float[] jitterRangesB = { 0.1f, 0.1f, 0.1f, 0.1f };

    public float minimumObjectSpacing = 20.0f;
    public bool normalDist = false;

    private float trackLenA;
    private float trackLenB;

    private List<int> trialNumbersA;
    private List<bool> blocksStableA;

    private List<int> trialNumbersB;
    private List<bool> blocksStableB;

    [HideInInspector] public List<int> trialContext = new List<int>();
    [HideInInspector] public List<bool> trialStable = new List<bool>();
    [HideInInspector] public List<int> trialNum = new List<int>();
    [HideInInspector] public List<int> trialBlockNums = new List<int>();
    [HideInInspector] public List<float[]> trialObjectPositionsRel = new List<float[]>();
    [HideInInspector] public List<float[]> trialObjectPositions = new List<float[]>();
    [HideInInspector] public List<int[]> trialObjectNums = new List<int[]>();

    [HideInInspector] public float[] trackOffsets;
    [HideInInspector] public float[] trackLens;

    private int totalTrials = 0;
    private int totalBlocks = 0;

    private int numTraversals = 0;

    private bool dispOutOfTrials = false;

    void Awake()
    {
        Debug.Log("start of ObjectSetHandler Awake");

        Debug.Log("there are " + allObjectCues.Count + " cues in the object cues list, check this is right.");
        if (allObjectCues.Count == 0)
        {
            Debug.Log("No object cues attached to object handler");
            UnityEditor.EditorApplication.isPlaying = false;
        }
        
        // Get some data from player controller
        trackLenA = playerController.trackLenA;
        trackLenB = playerController.trackLenB;
        trackLens = playerController.trackLens;
        trackOffsets = playerController.trackOffsets;

        trialNumbersA = playerController.trialNumbersA;
        trialNumbersB = playerController.trialNumbersB;
        blocksStableA = playerController.blocksStableA;
        blocksStableB = playerController.blocksStableB;

        if (((int)trialNumbersA.Count != (int)blocksStableA.Count) || ((int)trialNumbersB.Count != (int)blocksStableB.Count))
        {
            Debug.Log("Error block numbers/blocks stable have different numbers");
            // Quit
        }

        //ValidateJitterRanges();

        //void GetAllObjPositions();
        
        int nBlocksA = trialNumbersA.Count;
        int nBlocksB = trialNumbersB.Count;

        int maxBlocks = (int)Mathf.Max((float)nBlocksA, (float)nBlocksB);        
        for (int eachBlockI = 0; eachBlockI < maxBlocks; eachBlockI++)
        {
            //Debug.Log("block " + eachBlockI);
            int trialsHereA = 0;
            bool stableHereA = true;
            if (eachBlockI < nBlocksA)
            {
                trialsHereA = trialNumbersA[eachBlockI];
                stableHereA = blocksStableA[eachBlockI];
            }

            // Add A trials
            if (trialsHereA > 0)
            {
                //Debug.Log("Adding A trials");
                for (int trialIa = 0; trialIa < trialsHereA; trialIa++)
                {
                    trialContext.Add(1);
                    trialStable.Add(stableHereA);
                    trialNum.Add(totalTrials);
                    totalTrials++;
                    trialBlockNums.Add(totalBlocks);

                    //int trialI = totalTrials-1;
                    //Debug.Log("trialI " + (trialI+1) + ", trialNum " + trialNum[trialI] + ", trialStable " + trialStable[trialI] + ", context " + trialContext[trialI] + ", trialBlockNums " + trialBlockNums[trialI]);
                }
                totalBlocks++;
            }

            int trialsHereB = 0;
            bool stableHereB = true;
            if (eachBlockI < nBlocksB)
            {
                trialsHereB = trialNumbersB[eachBlockI];
                stableHereB = blocksStableB[eachBlockI];
            }

            if (trialsHereB > 0)
            {
                //Debug.Log("Adding B trials");
                for (int trialIb = 0; trialIb < trialsHereB; trialIb++)
                {
                    trialContext.Add(2);
                    trialStable.Add(stableHereB);
                    trialNum.Add(totalTrials);
                    totalTrials++;
                    trialBlockNums.Add(totalBlocks);

                    //int trialI = totalTrials-1;
                    //Debug.Log("trialI " + (trialI+1) + ", trialNum " + trialNum[trialI] + ", trialStable " + trialStable[trialI] + ", context " + trialContext[trialI] + ", trialBlockNums " + trialBlockNums[trialI]);
                }
                totalBlocks++;
            }
        }

        // Object Positions and identities each trial
        for (int trialI = 0; trialI < totalTrials; trialI++)
        {
            int contextH = trialContext[trialI];

            int[] objectsH = objectSetA;
            float[] positionsRelRef = (float[])positionsRelA.Clone(); 
            float[] jitterRangesH = (float[])jitterRangesA.Clone();
            if (contextH == 2) 
            { 
                objectsH = objectSetB;
                positionsRelRef = (float[])positionsRelB.Clone();
                jitterRangesH = (float[])jitterRangesB.Clone();
            }

            float[] positionsRelH = positionsRelRef;
            
            // If unstable, generate random positions
            if (trialStable[trialI] == false)
            {
                
                for (int objI = 0; objI < objectsH.Length; objI++)
                {
                    positionsRelH[objI] = positionsRelRef[objI] + (Random.Range(0.0f, jitterRangesH[objI]) - (jitterRangesH[objI] / 2.0f));
                }
            }

            float[] positionsAbsH = new float[objectsH.Length];
            for (int objI=0; objI < objectsH.Length; objI++) { positionsAbsH[objI] = positionsRelH[objI] * trackLens[trialContext[trialI] - 1]; }
            
            trialObjectPositions.Add(positionsAbsH);
            trialObjectPositionsRel.Add(positionsRelH);
            trialObjectNums.Add(objectsH);
        }

        /*
        int[] testTrials = { 0, 4, 6, 12 };
        foreach(int x in testTrials)
        {
            Debug.Log("trial " + x + " stable: " + trialStable[x]);
            PrintFloatArr(trialObjectPositionsRel[x]);
            PrintFloatArr(trialObjectPositions[x]);
        }
        */
        
    }

    void Start()
    {
        Debug.Log("start of ObjectSetHandler Start; HAS TO RUN AFTER PLAYER CONTROLLER!");

        SetTrialObjectPositions();
    }

    // Update is called once per frame
    void Update()
    {
        numTraversals = playerController.numTraversals;
        if (numTraversals < trialContext.Count)
        {
            SetTrialObjectPositions();
        }
        else
        {
            if (dispOutOfTrials == false)
            { 
                Debug.Log("Out of trials to set objects; numTraversals " + numTraversals + ", trials made " + trialContext.Count);
                dispOutOfTrials = true;
            }
        }
    }

    public void SetTrialObjectPositions()//allObjectCues[objI].transform.position = hiddenObjPosition;
    {
        ResetAllObjects();

        int[] objectsH = trialObjectNums[numTraversals];
        float[] positionsH = trialObjectPositions[numTraversals];

        float xOffsetH = trackOffsets[trialContext[numTraversals] - 1];
        for (int objI = 0; objI < objectsH.Length; objI++)
        {
            int thisObj = objectsH[objI];
            Vector3 currentObjPosition = allObjectCues[thisObj].transform.position;
            Vector3 newObjPosition = new Vector3(xOffsetH, currentObjPosition.y, positionsH[objI]);
            allObjectCues[thisObj].transform.position = newObjPosition;
        }
    }

    void ResetAllObjects()
    {
        for (int objI=0; objI < allObjectCues.Count; objI++)
        {
            Vector3 currentObjPosition = allObjectCues[objI].transform.position;
            Vector3 hiddenObjPosition = new Vector3( 0.0f, currentObjPosition.y, -500.0f );
            allObjectCues[objI].transform.position = hiddenObjPosition;
        }
        //Debug.Log("Made it through object resetting");
    }

    void PrintFloatArr(float[] floatArr)
    {
        string strstr = ""; foreach (float x in floatArr) { strstr = string.Concat(strstr, x.ToString(), "; "); }
        Debug.Log(strstr);
    }

    List<float> ValidateJitterRanges(List<float> objPositionsRel, float minObjSpacing)
    {
        int nObj = objPositionsRel.Count;
        /*
        for (int objI = 0; objI < 5; objI++)
        {
            bool rangeGood = true;

            // Check it's not < 0
            if (minRanges[objI] < 0.0f)
            {
                rangeGood = false;
                minRanges[objI] = 0.0f;
            }
            if (maxRanges[objI] > trackLen)
            {
                rangeGood = false;
                maxRanges[objI] = trackLen;
            }
            if (objI < 5)
            {
                float rangeDiff = Mathf.Abs(maxRanges[objI] - minRanges[objI + 1]);
                if (rangeDiff > minimumObjectSpacing)
                {

                }
            }
            if (objI > 0)
            {
                float rangeDiff = Mathf.Abs(minRanges[objI] - maxRanges[objI - 1]);
                if (rangeDiff > minimumObjectSpacing)
                {

                }
            }

        }
        */
        return objPositionsRel;
    }
}