using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
//using Uniduino;
using System.IO;
using System.IO.Ports;
using System.Threading;
// for sending email when session is done
//using System.Net;
//using System.Net.Mail;
//using System.Net.Security;
//using System.Security.Cryptography.X509Certificates;

public class PlayerController_EpisodeBlocks1 : MonoBehaviour
{
	// arduino
	/*
	public Arduino arduino;
	private int rewardPin = 12; // for administering rewards
	private int stopPin = 9; // for stopping recording at end of session
	*/
	public string thisPort = "COM4";
	private SerialPort _serialPort;
	private int delay;

	public string rewardPort = "COM5";
	private SerialPort _serialPortReward;
	public enum ArduinoUse { Uniduino, SimulatedRunning };
	public ArduinoUse ArdUse;
	[HideInInspector]
	public bool simulatedRunning = false;
	public float simulatedRunningSpeed = 75.0f;
	private float startOffsetRange;

	public int SessionDay = 1;
	public int CueSet = 1;

	// movement / trial tracking
	private float initialPosition_z = 0.0f; // The Start of the track
	public float startOffsetStart = -110.0f;   // Upper bound on a random starting offset
	public float startOffsetEnd = -50.0f;   // Lower bound on a random starting offset
	private Vector3 initialPosition;
	private Vector3 lastPosition;
	private Vector3 blackoutBoxPosition = new Vector3(0f, -24.0f, 526.0f);
	public int numTraversals = 0;
	private int numTrialsPerBlock;

	private string lick_raw;
	public int lickPinValue;
	private int numLicks = 0;
	public int lickFlag = 0;
	private int rotaryTicks = 0;
	private float speed = 0.0447f;
	private int syncPinState;
	//private int arduinoRespondedFlag = 0;

	// Remapping stuff
	public int numBaselineTrials = 40; // How many trials to start the session with repeatable cues order
	public int numTrialsMin = 210;     // Block sizes are random, so we need a range for final number of trials
	public int numTrialsMax = 250;
	public int minBlockSize = 20;      // Min number of trials for random blocks
	public int maxBlockSize = 35;      // Max number of trials for random blocks
	public int numBaselineRepeats = 2;  // How many times to work in the baseline cue order 
	[HideInInspector]
	public int numTrialsTotal;


	public float rewardLocRel = 0.6f;

	public int[] baselineCueOrder = { 1, 2, 3, 4, 5 }; // The cue order to use as a baseline, repeated multiple times each session
	//public int[] familiarCueOrder = { 3, 4, 1, 5, 2 };//{2, 4, 1, 5, 3};  The cue order to repeat once each session
	[HideInInspector]
	public int[] familiarCueOrder = new int[5];
	[HideInInspector]
	public List<int> cueOrderList = new List<int>();
	[HideInInspector]
	int[] cueOrderNow = new int[5];

	public int[,,] dayCueOrders = new int[3,8,5]; //[SessionDay, currentCueOrder - 2, i]
	/*
public int[,,] dayCueOrders = new int[,,]
		{
			{
				{ 5, 3, 1, 4, 2 },
				{ 5, 3, 4, 1, 2 },
				{ 4, 2, 3, 5, 1 },
				{ 4, 5, 3, 1, 2 },
				{ 2, 1, 3, 5, 4 },
				{ 3, 1, 5, 2, 4 },
				{ 2, 5, 1, 3, 4 },
				{ 3, 2, 4, 1, 5 },
				{ 5, 2, 4, 3, 1 },
				{ 3, 5, 2, 1, 4 }
			},
			{
				{ 1, 5, 4, 3, 2 },
				{ 5, 3, 2, 4, 1 },
				{ 2, 1, 4, 3, 5 },
				{ 1, 5, 3, 2, 4 },
				{ 4, 1, 5, 3, 2 },
				{ 3, 2, 5, 4, 1 },
				{ 1, 2, 5, 3, 4 },
				{ 2, 3, 5, 1, 4 },
				{ 4, 2, 1, 3, 5 },
				{ 2, 3, 1, 4, 5 }
			},
			{
				{ 1, 5, 2, 4, 3 },
				{ 4, 1, 2, 5, 3 },
				{ 5, 1, 2, 3, 4 },
				{ 5, 4, 2, 1, 3 },
				{ 1, 3, 2, 5, 4 },
				{ 1, 3, 4, 2, 5 },
				{ 2, 3, 4, 5, 1 },
				{ 5, 2, 1, 4, 3 },
				{ 2, 5, 3, 4, 1 },
				{ 2, 5, 4, 1, 3 }
			}
		}
	*/

	[HideInInspector]
	public List<int> trialCueOrder; // Which cue order to use on each trial
	[HideInInspector]
	public int numberDayCueOrders = 10; // number of lists to draw from for each day


	public float[] objectPositionsRel = { 0.1f, 0.23f, 0.5f, 0.78f, 0.9f }; // This determined by 
	public GameObject Object1;
	public GameObject Object2;
	public GameObject Object3;
	public GameObject Object4;
	public GameObject Object5;
	private List<GameObject> ObjectCues = new List<GameObject>();


	// for calculating sequence of reward locations
	private List<int> rewardTrials; // trial num for each reward
	private List<int> blockType;
	private int rewardCount = 0;
	public float[] rewardCenters;
	//public float[] blockType;

	// for each trial's reward
	public int rewardTrial;
	public float rewardPosition;
	public float rewardZoneStart;
	public float rewardZoneEnd;
	public float rewardZoneBuffer = 15f;
	private bool lickForReward;
	private int rewardFlag = 0;
	private int automaticRewardFlag = 1;


	// for detecting licks
	//private DetectLicks lickScript;

	// reminders
	private int numReminderTrials;
	private int numReminderTrialsEnd;
	private int numReminderTrialsBegin;
	private int reminderFlag;

	// gain manipulations
	private bool manipSession;
	private int numGainTrials;
	private float endGain;
	public float gain = 1f;
	private float[] gains;
	private int[] order;
	private int numGainRepetitions;
	private float[] gainTrialSequence;

	// Timeout box
	public bool UseTimeout = true;
	public float TimeoutMinimum = 3.0f;
	public float TimeoutMaximum = 5.5f;
	public float BlackRunningSeconds = 600.0f;
	private bool allowLapEnd = true;
	private float timeoutOver;
	private float timeoutDelay;
	private bool haveSetTimeoutEnd = false;
	[HideInInspector]
	public bool allowMovement = true;

	// saving data
	private string localDirectory;
	private string serverDirectory;
	private SessionParams paramsScript;
	private string mouse;
	private string session;
	private bool saveData;
	private string trialTimesFile;
	private string serverTrialTimesFile;
	private string rewardFile;
	private string serverRewardFile;
	private string lickFile;
	private string serverLickFile;
	private string positionFile;
	private string serverPositionFile;
	private string startStopFile;
	private string paramsTwoFile;
	private string trialListFile;

	// teleport
	private float teleportPosition;
	//private int teleportFlag = 0;
	//teleportPosition = paramsScript.trackEnd;
	private int trackLen;
	private bool justTeleported = false;

	private int cmdWrite = 0;
	private int pulseMarker = 1;
	private bool triggerStopSession = false;
	private bool sentStartSignal = false;
	private int startStopPinState;
	private bool recordingStarted = false;

	private int target = 30;

	private StreamWriter sw_lick;
	private StreamWriter sw_pos;
	private StreamWriter sw_trial;
	private StreamWriter sw_reward;
	private StreamWriter sw_startstop;
	private StreamWriter sw_par;
	private StreamWriter sw_trialList;
	private bool missedArdFrame;
	private StreamWriter sw_trialOrder;

	private bool endSessionE = false;
	private bool endSessionU = false;
	private bool dispEndSessE = false;
	private bool dispEndSessU = false;
	private bool ifSetSkipToEnd = false;

	private void Awake()
	{
		Debug.Log("wake");
		if (!((SessionDay == 1) || (SessionDay == 2) || (SessionDay == 3))){
			Debug.Log("Bad session day integer, quitting");
			UnityEditor.EditorApplication.isPlaying = false;
		}

		// initialize arduino
		if (ArdUse == ArduinoUse.Uniduino)
		{
			// initialize arduino
			try
			{
				connect(thisPort, 115200, true, 10);
				Debug.Log("Connected to lick and sync ports");
			}
			catch
			{
				Debug.Log("failed to connect to lick and sync ports");
			}
		}
		else
		{
			Debug.Log("*Configuring fake pins*");
			simulatedRunning = true;
		}

		if (ArdUse == ArduinoUse.Uniduino)
		{
			Debug.Log("Attempting reward port communication");
			try
			{
				connectRwd(rewardPort, 115200, true, 10);
				Debug.Log("Connected to reward port");
			}
			catch
			{
				Debug.Log("failed to connect to reward port");
			}
		}
		else
		{
			Debug.Log("skipping reward port");
		}

		try
		{
			ObjectCues.Add(Object1);
			ObjectCues.Add(Object2);
			ObjectCues.Add(Object3);
			ObjectCues.Add(Object4);
			ObjectCues.Add(Object5);
		}
		catch
		{
			Debug.Log("Failed to add objects to list");
		}


		if (ArdUse == ArduinoUse.Uniduino)
		{
			_serialPort.DiscardInBuffer();
			_serialPort.DiscardOutBuffer();

			_serialPortReward.DiscardInBuffer();
			_serialPortReward.DiscardOutBuffer();
		}

		Debug.Log("end of wake");
	}

	void Start()
	{
		Debug.Log("Begin Start");

		if (ArdUse == ArduinoUse.Uniduino)
		{
			Debug.Log("Handshaking Arduino connection for lick and sync");
			bool connectedArd = false;
			int triesA = 0;
			float TimeStartedConn = Time.realtimeSinceStartup;
			while (connectedArd == false)
			{
				triesA++;
				cmdWrite = 10;
				_serialPort.Write(cmdWrite.ToString() + ',');
				try
				{
					string shake_check = _serialPort.ReadLine();
					int shake_value = int.Parse(shake_check);
					if (shake_value == 10)
					{
						Debug.Log("Got a 10 back, handshook the arduino " + triesA + " tries, time " + (Time.realtimeSinceStartup - TimeStartedConn));
						connectedArd = true;
					}
				}
				catch
				{

				}

				if (Input.GetKeyDown(KeyCode.X))
				{
					Debug.Log("Forcing quit from arduino handshake loop");
					UnityEditor.EditorApplication.isPlaying = false;
				}
			}
		}

		// get session parameters from SessionParams script
		GameObject player = GameObject.Find("Player");
		paramsScript = player.GetComponent<SessionParams>();

		// Set up cue lists to use
		// 1 means baseline order; 2 means familiar order; 
		// 3 means pick from the array of cue orders
		List<int> blockSizes = new List<int>();
		List<int> orderUse = new List<int>();
		int nBlocks = 0;
		blockSizes.Add(numBaselineTrials);
		orderUse.Add(1);
		int cueListSize = numBaselineTrials;
		nBlocks++;

		if (CueSet == 1)
		{
			familiarCueOrder = new int[] { 3, 4, 1, 5, 2 };
			dayCueOrders = new int[,,]
			{
				{
					{ 5, 3, 1, 4, 2 },
					{ 5, 3, 4, 1, 2 },
					{ 4, 2, 3, 5, 1 },
					{ 4, 5, 3, 1, 2 },
					{ 2, 1, 3, 5, 4 },
					{ 3, 1, 5, 2, 4 },
					{ 2, 5, 1, 3, 4 },
					{ 3, 2, 4, 1, 5 },
					{ 5, 2, 4, 3, 1 },
					{ 3, 5, 2, 1, 4 }
				},
				{
					{ 1, 5, 4, 3, 2 },
					{ 5, 3, 2, 4, 1 },
					{ 2, 1, 4, 3, 5 },
					{ 1, 5, 3, 2, 4 },
					{ 4, 1, 5, 3, 2 },
					{ 3, 2, 5, 4, 1 },
					{ 1, 2, 5, 3, 4 },
					{ 2, 3, 5, 1, 4 },
					{ 4, 2, 1, 3, 5 },
					{ 2, 3, 1, 4, 5 }
				},
				{
					{ 1, 5, 2, 4, 3 },
					{ 4, 1, 2, 5, 3 },
					{ 5, 1, 2, 3, 4 },
					{ 5, 4, 2, 1, 3 },
					{ 1, 3, 2, 5, 4 },
					{ 1, 3, 4, 2, 5 },
					{ 2, 3, 4, 5, 1 },
					{ 5, 2, 1, 4, 3 },
					{ 2, 5, 3, 4, 1 },
					{ 2, 5, 4, 1, 3 }
				}
			};
		}
		else if(CueSet == 2)
        {
			familiarCueOrder = new int[] {2, 4, 1, 5, 3};
			dayCueOrders = new int[,,]
			{
				{
					{ 3, 1, 4, 5, 2 },
					{ 4, 5, 1, 2, 3 },
					{ 1, 5, 2, 4, 3 },
					{ 2, 5, 1, 3, 4 },
					{ 3, 1, 2, 4, 5 },
					{ 1, 5, 4, 3, 2 },
					{ 2, 3, 5, 1, 4 },
					{ 4, 3, 2, 1, 5 },
					{ 5, 1, 4, 2, 3 },
					{ 2, 3, 1, 4, 5 }
				},
				{
					{ 4, 3, 5, 2, 1 },
					{ 2, 1, 4, 3, 5 },
					{ 5, 4, 3, 2, 1 },
					{ 1, 5, 3, 2, 4 },
					{ 5, 1, 2, 3, 4 },
					{ 3, 2, 4, 1, 5 },
					{ 3, 2, 4, 1, 5 },
					{ 2, 4, 5, 3, 1 },
					{ 4, 2, 3, 5, 1 },
					{ 3, 5, 4, 2, 1 }
				},
				{
					{ 3, 4, 2, 5, 1 },
					{ 1, 3, 5, 4, 2 },
					{ 5, 4, 2, 1, 3 },
					{ 5, 3, 4, 1, 2 },
					{ 3, 4, 1, 2, 5 },
					{ 3, 4, 5, 1, 2 },
					{ 3, 1, 5, 2, 4 },
					{ 2, 5, 3, 4, 1 },
					{ 5, 3, 1, 2, 4 },
					{ 5, 4, 1, 3, 2 }
				}
			};

		}

		var random = new System.Random();
		while (cueListSize < (numTrialsMin))
		{
			// Set up how many trials each block will be
			bool goodRandomSize = false;
			while (goodRandomSize == false)
			{
				int rNum = random.Next(minBlockSize, maxBlockSize);
				if ((cueListSize + rNum) <= numTrialsMax)
				{
					goodRandomSize = true;

					blockSizes.Add(rNum);
					bool haveUniqueBlock = false;
					int blockAdd = 0;
					while (haveUniqueBlock == false)
					{
						blockAdd = random.Next(3, numberDayCueOrders + 2);
						int matchingBlocks = 0;
						for (int i = 0; i < nBlocks; i++)
						{
							matchingBlocks = matchingBlocks + Convert.ToInt32(orderUse[i] == blockAdd);
						}
						if (matchingBlocks == 0)
						{
							haveUniqueBlock = true;
						}
					}

					orderUse.Add(blockAdd);
					// These need to not repeat also...

					nBlocks++;
					cueListSize = cueListSize + rNum;
				}
			}
		}

		/*
		Debug.Log("Order before familiar/baseline insertions");
		for (int j = 0; j < nBlocks; j++)
		{
			Debug.Log("slot " + j + " order " + orderUse[j]);
		}
		*/

		// Assign numBaselineBlocks (repeats) randomly into blockList, not adjacent to each other
		List<int> attemptList = orderUse;
		int goodBaselines = 0;
		for (int baseRep = 0; baseRep < numBaselineRepeats; baseRep++)
		{
			bool slotGood = false;
			for (int attemptI = 0; attemptI < 50; attemptI++)
			{
				if (slotGood == false)
				{
					int repBlock = random.Next(2, nBlocks - 1);
					if ((attemptList[repBlock - 1] != 1) && (attemptList[repBlock + 1] != 1) && (attemptList[repBlock] != 1))
					{
						attemptList[repBlock] = 1;
						slotGood = true;
					}
				}
			}
			if (slotGood == false)
            {
				for (int againAttempt = 0; againAttempt < 100; againAttempt++)
                {
					if (slotGood == false)
					{
						int repBlock = random.Next(2, nBlocks - 1);
						if (attemptList[repBlock] != 1)
						{
							attemptList[repBlock] = 1;
							slotGood = true;
						}
					}
				}
            }
			if (slotGood == true)
            {
				goodBaselines++;
            }
		}
		bool famSlot = false;
		for (int famAttempt = 0; famAttempt < 100; famAttempt++)
        {
			if (famSlot == false)
			{
				int repBlock = random.Next(2, nBlocks - 1);
				if (attemptList[repBlock] != 1)
                {
					attemptList[repBlock] = 2;
					famSlot = true;
                }
			}
		}
		Debug.Log("baseline reps = " + goodBaselines + ", requested " + numBaselineRepeats + "; famDone " + famSlot);
		orderUse = attemptList;	

		//Debug.Log("Order after adding in baseline/familiar blocks");
		for (int j = 0; j < nBlocks; j++)
		{
			//Debug.Log("slot " + j + " order " + orderUse[j]);
		}
		
		// Go through these and make the whole list
		
		int listPos = 0;
		for (int blockI = 0; blockI < nBlocks; blockI++)
		{
			for (int trialI = 0; trialI < blockSizes[blockI]; trialI++)
			{
				cueOrderList.Add(orderUse[blockI]);
				listPos++;
			}
		}
		numTrialsTotal = cueListSize;
		

		// Validate cue indexing 
		/*
		for (int blockI = 0; blockI < nBlocks; blockI++)
		{
			if (orderUse[blockI] == 1)
			{
				Debug.Log("block " + blockI + ": " + baselineCueOrder[0] + " " + baselineCueOrder[1] + " " + baselineCueOrder[2] + " " + baselineCueOrder[3] + " " + baselineCueOrder[4]);
			}
			else if (orderUse[blockI] == 2)
            {
				Debug.Log("block " + blockI + ": " + familiarCueOrder[0] + " " + familiarCueOrder[1] + " " + familiarCueOrder[2] + " " + familiarCueOrder[3] + " " + familiarCueOrder[4]);
			}
			else if (orderUse[blockI] > 2)
            {
				int vv = orderUse[blockI]-2;
				int ss = SessionDay - 1;
				Debug.Log("block " + blockI + " order " + (vv+1) + ": " + dayCueOrders[ss, vv, 0] + " " + dayCueOrders[ss, vv, 1] + " " + dayCueOrders[ss, vv, 2] + " " + dayCueOrders[ss, vv, 3] + " " + dayCueOrders[ss, vv, 4]); // sheet, row, column
			}
		}
		*/
		Debug.Log("num trials total: " + numTrialsTotal);

		initialPosition_z = paramsScript.trackStart;
		teleportPosition = paramsScript.trackEnd;
		trackLen = (int)teleportPosition;

		startOffsetRange = startOffsetEnd - startOffsetStart;
		
		float randOffset = UnityEngine.Random.Range(startOffsetStart, startOffsetEnd);
		Debug.Log("Lap " + (numTraversals+1) + " start offset " + randOffset + ", pos " + (initialPosition_z + randOffset));

		initialPosition = new Vector3(0.0f, 0.5f, initialPosition_z + randOffset);
		transform.position = initialPosition;
		lastPosition = transform.position;

		// reward params
		lickForReward = paramsScript.lickForReward;
			
		// DEFINE REWARD SEQUENCE
		rewardCenters = new float[numTrialsTotal];
		blockType = new List<int>();
		rewardTrials = new List<int>();

		SetObjectPositions();
		
		// For debugging purposes, print this to file to check
		/*
		Debug.Log("Writing cue order list to file");
		sw_trialOrder = new StreamWriter(localDirectory + "\\" + mouse + "\\" + session + "\\" + "TrialOrder.txt", true);
		sw_trialOrder.WriteLine("Solved num trials = " + numTrialsTotal);
		sw_trialOrder.WriteLine("listPos num trials = " + listPos);
		sw_trialOrder.WriteLine("cueOrderList count = " + cueOrderList.Count);
		for (int j = 0; j < nBlocks; j++)
		{
			sw_trialOrder.WriteLine("slot " + j + " order " + orderUse[j] + " block length " + blockSizes[j]);
		}
		for (int ttI = 0; ttI < numTrialsTotal; ttI++)
		{
			sw_trialOrder.WriteLine(ttI + " " + cueOrderList[ttI]);
		}
		sw_trialOrder.Close();
		*/
		
		// Set reward boundaries
		rewardTrial = 1;
		rewardPosition = trackLen * rewardLocRel;
		rewardZoneStart = rewardPosition - rewardZoneBuffer;
		rewardZoneEnd = rewardPosition + rewardZoneBuffer;
			
		// for saving data	
		mouse = paramsScript.mouse;
		session = paramsScript.session;
		saveData = paramsScript.saveData;
		
		
		if (saveData == true)
		{
			localDirectory = paramsScript.localDirectory;
			serverDirectory = paramsScript.serverDirectory;
			trialTimesFile = localDirectory + "\\" + mouse + "\\" + session + "_trial_times.txt";
			serverTrialTimesFile = serverDirectory + "\\" + mouse + session + "_trial_times.txt";
			rewardFile = localDirectory + "\\" + mouse + "\\" + session + "_reward.txt";
			serverRewardFile = serverDirectory + "\\" + mouse + session + "_reward.txt";
			positionFile = localDirectory + "\\" + mouse + "\\" + session + "_position.txt";
			serverPositionFile = serverDirectory + "\\" + mouse + "\\" + session + "_position.txt";
			lickFile = localDirectory + "\\" + mouse + "\\" + session + "_licks.txt";
			serverLickFile = serverDirectory + "\\" + mouse + "\\" + session + "_licks.txt";
			startStopFile = localDirectory + "\\" + mouse + "\\" + session + "_startStop.txt";
			paramsTwoFile = localDirectory + "\\" + mouse + "\\" + session + "_params2.txt";
			trialListFile = localDirectory + "\\" + mouse + "\\" + session + "_trialList.txt";

			sw_lick = new StreamWriter(lickFile, true);
			sw_pos = new StreamWriter(positionFile, true);
			sw_trial = new StreamWriter(trialTimesFile, true);
			sw_reward = new StreamWriter(rewardFile, true);
			sw_startstop = new StreamWriter(startStopFile, true);
			sw_par = new StreamWriter(paramsTwoFile, true);
			sw_trialList = new StreamWriter(trialListFile, true);

			for (int blockI = 0; blockI < nBlocks; blockI++)
			{
				if (orderUse[blockI] == 1)
				{
					//sw_par.Write(Time.realtimeSinceStartup + "\t" + automaticRewardFlag + "\t" + numTraversals + "\t" + blockType[numTraversals] + "\n");
					sw_par.Write("block " + blockI + " order b: " + baselineCueOrder[0] + "\t" + baselineCueOrder[1] + "\t" + baselineCueOrder[2] + "\t" + baselineCueOrder[3] + "\t" + baselineCueOrder[4] + "\n");
				}
				else if (orderUse[blockI] == 2)
				{
					sw_par.Write("block " + blockI + " order f: " + familiarCueOrder[0] + "\t" + familiarCueOrder[1] + "\t" + familiarCueOrder[2] + "\t" + familiarCueOrder[3] + "\t" + familiarCueOrder[4] + "\n");
				}
				else if (orderUse[blockI] > 2)
				{
					int vv = orderUse[blockI] - 2;
					int ss = SessionDay - 1;
					sw_par.Write("block " + blockI + " order " + (vv + 1) + ": " + dayCueOrders[ss, vv, 0] + "\t" + dayCueOrders[ss, vv, 1] + "\t" + dayCueOrders[ss, vv, 2] + "\t" + dayCueOrders[ss, vv, 3] + "\t" + dayCueOrders[ss, vv, 4] + "\n");
				}
			}
			sw_par.Close();

			for (int ttI = 0; ttI < cueListSize; ttI++)
            {
				sw_trialList.Write("trial " + ttI + "\t" + cueOrderList[ttI] + "\n");
            }
			sw_trialList.Close();

		}
		else
        {
			Debug.Log("Warning: this is NOT saving any data");
        }
		
		Debug.Log("Press return to start the session");

		if (Application.targetFrameRate != target)
			Application.targetFrameRate = target;
	}

	void Update()
	{
		missedArdFrame = false;
		if (Application.targetFrameRate != target)
			Application.targetFrameRate = target;

		if (recordingStarted == false)
        {
			if (Input.GetKeyDown(KeyCode.Return))
			{
				Debug.Log("starting Update");
				Debug.Log("numTraversals = " + numTraversals + " / numTrialsTotal = " + numTrialsTotal);
				Debug.Log("Trial " + (numTraversals + 1) + ", gain = " + gain + ", reward position = " + rewardPosition);
				recordingStarted = true;
			}
		}

		if (recordingStarted == true)
		{
			
			if (sentStartSignal == false)
			{
				cmdWrite = 4;
				if (ArdUse == ArduinoUse.Uniduino)
				{
					_serialPort.Write(cmdWrite.ToString() + ',');
					_serialPort.DiscardInBuffer();
					_serialPort.DiscardOutBuffer();
				}
				sentStartSignal = true;
				if (saveData == true)
				{
					sw_startstop.Write("StartSignal" + "\t" + Time.realtimeSinceStartup + "\n");
				}

				if (ArdUse == ArduinoUse.Uniduino)
				{
					_serialPortReward.DiscardInBuffer();
					_serialPortReward.DiscardOutBuffer();
				}
			}

			cmdWrite = 1;
			
			if (Input.GetKeyDown(KeyCode.Space))
			{
				//StartCoroutine( Reward ());
				cmdWrite = 2;
				_serialPort.Write(cmdWrite.ToString() + ',');
				cmdWrite = 1;
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
				

			// reset teleport and reward flags in appropriate zone
			if (transform.position.z < (rewardZoneStart - 20))
			{
				rewardFlag = 0;
			}
			if (transform.position.z < rewardZoneStart - 20)
			{
				//teleportFlag = 0;
			}

			// reward
			// always allow animal to lick for reward in zone if box checked
			if (rewardFlag == 0)
			{
				if (lickForReward & transform.position.z > rewardZoneStart & transform.position.z < rewardZoneEnd)
				{
					if (lickFlag == 1)
					{
						rewardFlag = 1;
						cmdWrite = 2;

						// update debug log and save info
						Debug.Log("Trial " + (numTraversals + 1) + ", reward pos=" + rewardPosition + ", automaticReward=" + automaticRewardFlag + ", reward requested");

						if (saveData)
						{
							//var sw = new StreamWriter(rewardFile, true);
							sw_reward.Write(Time.realtimeSinceStartup + "\t" + rewardTrial + "\t" + rewardPosition + "\t" + 0 + "\t" + 0 + "\n");
							//sw_reward.Close();
						}

						rewardCount += 1;
					}
				}

				// automatic reward
				if (automaticRewardFlag == 1 & (transform.position.z > rewardPosition))
				{
					rewardFlag = 1;
					cmdWrite = 2;

					// update debug log
					Debug.Log("Trial " + (numTraversals + 1) + ", reward pos=" + rewardPosition + ", automaticReward=" + automaticRewardFlag + ", auto reward delivery");

					if (saveData)
					{
						//var sw_reward = new StreamWriter(rewardFile, true);
						sw_reward.Write(Time.realtimeSinceStartup + "\t" + rewardTrial + "\t" + rewardPosition + "\t" + 1 + "\t" + 0 + "\n");
						//sw_reward.Close();
					}

					// update reward zone
					rewardCount += 1;
				}
			}
			
			// teleport
			if ((transform.position.z > teleportPosition)) // | (transform.position.z < timeoutLimit)
			{
				allowLapEnd = true;
				//Debug.Log(UseTimeout);

				if (endSessionU == true)
				{
					if (ifSetSkipToEnd == false)
					{
						Debug.Log("Recieved keypress sequence E - U, skipping to end of session");
						numTraversals = numTrialsTotal - 1;
						ifSetSkipToEnd = true;
					}
				}

				if (UseTimeout == true)
				{

					if (haveSetTimeoutEnd == false)
					{
						if (numTraversals + 1 == numTrialsTotal)
						{
							timeoutDelay = BlackRunningSeconds;
							Debug.Log("starting dark running");
						}
						else
						{
							timeoutDelay = UnityEngine.Random.Range(TimeoutMinimum, TimeoutMaximum);
						}
						timeoutOver = Time.realtimeSinceStartup + timeoutDelay;
						haveSetTimeoutEnd = true;
						Debug.Log("Time out for " + timeoutDelay + "s");
						//Debug.Log(Time.realtimeSinceStartup + ", " + timeoutOver);
						try
						{
							Debug.Log("This was at " + System.DateTime.Now);
						}
						catch
						{

						}
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
						Debug.Log("Done timeout");
					}
				}

				if (allowLapEnd == true)
				{
					Debug.Log("ending lap");
					// update number of trials
					numTraversals += 1; 
					// At the end of the last lap, this causes it to tick up to num trials total, which with 0-intexing is one more than the listed number of trials
					Debug.Log("numTraversals = " + numTraversals + " / numTrialsTotal = " + numTrialsTotal);
					//teleportFlag = 1;

					// set automaticRewardFlag to 1 if the upcoming trial will be automatic reward
					automaticRewardFlag = 1;

					// TELEPORT
					float randOffset = UnityEngine.Random.Range(startOffsetStart, startOffsetEnd);
					Debug.Log("Lap " + (numTraversals+1) + " start offset " + randOffset + ", pos " + (initialPosition_z + randOffset));

					initialPosition[2] = initialPosition_z+randOffset;
					
					transform.position = initialPosition;

					lastPosition = transform.position;
					justTeleported = true;

					rewardFlag = 0;
					//Debug.Log("set reward flag to 0");

					// write trial time to file
					if (numTraversals < numTrialsTotal)
					{
						// Update cue object positions
						SetObjectPositions();

						// update contrast and gain
						gain = 1;

						// update debug log
						Debug.Log("Trial " + (numTraversals + 1) + ", gain = " + gain + ", reward position = " + rewardPosition);

						if (saveData)
						{
							//var sw = new StreamWriter(trialTimesFile, true);
							sw_trial.Write(Time.realtimeSinceStartup + "\t" + automaticRewardFlag + "\t" + numTraversals + "\n"); //"\t" + blockType[numTraversals] + 
						}

						if (numTraversals == (numTrialsTotal - 1))
                        {
							Debug.Log("this was the second to last lap");
                        }
					}
					else
					{
						// end session after appropriate number of trials
						Debug.Log("this was the last lap, numTraversals = " + numTraversals);
						triggerStopSession = true;
						Debug.Log("trigger stop session = " + triggerStopSession);
					}

				}
			}
			
			if (ArdUse == ArduinoUse.Uniduino)
			{
				_serialPort.Write(cmdWrite.ToString() + ',');
				try
				{
					lick_raw = _serialPort.ReadLine();
					string[] lick_list = lick_raw.Split('\t');
					lickPinValue = int.Parse(lick_list[0]);
					if (lickPinValue > 500 & lickFlag == 1)
					{
						lickFlag = 0;
					}
					if (lickPinValue < 500 & lickFlag == 0)
					{
						numLicks += 1;
						lickFlag = 1;

						if (saveData)
						{
							//var sw_lick = new StreamWriter(lickFile, true);
							sw_lick.Write(transform.position.z + "\t" + Time.realtimeSinceStartup + "\n");
							//sw_lick.Close();
						}
					}

					rotaryTicks = int.Parse(lick_list[1]);
					syncPinState = int.Parse(lick_list[2]);
					startStopPinState = int.Parse(lick_list[3]);
				}
				catch (TimeoutException)
				{
					Debug.Log("lickport/encoder timeout on frame " + Time.frameCount);
					missedArdFrame = true;
                }
				catch
				{
					Debug.Log("cmd " + cmdWrite + " reply " + lick_raw + " " + Time.frameCount);
					missedArdFrame = true;
				}

				if (cmdWrite == 2)
				{
					try
					{
						_serialPortReward.Write(cmdWrite.ToString() + ',');
						//missedReward = false;
						//_serialPortReward.DiscardInBuffer();
						//_serialPortReward.DiscardOutBuffer();
					}
					catch
					{
						Debug.Log("failed to write reward command");
						//missedReward = true; // This just forces a reward delivery on the next frame, regardless of what happened here
					}
				}
			}
			
			if (ArdUse == ArduinoUse.Uniduino)
			{
				_serialPort.DiscardInBuffer();
				_serialPort.DiscardOutBuffer();

				_serialPortReward.DiscardInBuffer();
				_serialPortReward.DiscardOutBuffer();
			}

			float delta_T = Time.deltaTime;
			float delta_z = rotaryTicks * speed;
			if (simulatedRunning == true)
            {
				delta_z = simulatedRunningSpeed * delta_T;
				rotaryTicks = 1;
            }

			if (justTeleported == true)
			{
				rotaryTicks = 0;
				justTeleported = false;
				delta_z = 0f;
			}
			
			float current_z = transform.position.z;
			if (allowMovement == true)
			{

				if (rotaryTicks == 0)
				{
					transform.position = lastPosition; // possible could get stuck here on a lap transition
				}
				else
				{
					transform.position = new Vector3(0.0f, 0.5f, current_z + delta_z);
				}
				lastPosition = transform.position;
            }

			//var sw_pos = new StreamWriter(positionFile, true);

			if (saveData == true)
			{ 
				try
				{
					int cueOrderPrint = 0;
					if (numTraversals < numTrialsTotal)
                    {
						cueOrderPrint = cueOrderList[numTraversals];

					}
					sw_pos.Write(transform.position.z + "\t" + Time.realtimeSinceStartup + "\t" + syncPinState + "\t" +
									pulseMarker + "\t" + startStopPinState + "\t" + delta_z + "\t" +
									lickPinValue + "\t" + rewardFlag + "\t" + numTraversals + "\t" + cueOrderPrint + "\t" + delta_T + "\t" + missedArdFrame + "\n");
				}
				catch
                {
					Debug.Log("this failed");
                }
			}
			
            // position (after transform), timeSinceStartup, syncPinState, pulseMarker, startStopPinState, delta_z, lickPinValue, rewardFlag, numTraversals, 
			
            pulseMarker = pulseMarker * -1;

			if (triggerStopSession)
			{
				Debug.Log("Stopping session at " + Time.realtimeSinceStartup + "\n");
				Debug.Log("Stopping 1 " + Time.realtimeSinceStartup);
				sw_startstop.Write("StopSignalPlanned" + "\t" + Time.realtimeSinceStartup + "\n");
				UnityEditor.EditorApplication.isPlaying = false;
			}
		}
		
	}

	void ConfigurePins () 
	{
		if (ArdUse == ArduinoUse.Uniduino)
		{
			//arduino.pinMode(rewardPin, PinMode.OUTPUT);
			//arduino.pinMode(stopPin, PinMode.OUTPUT);
			Debug.Log("Pins configured (player controller)");
		}
		else
        {
			Debug.Log("Using simulated running");
        }
	}
	void UpdateCueOrder ()
    {
		int currentCueOrder = cueOrderList[numTraversals];

		switch (currentCueOrder)
		{
			case 1:
				cueOrderNow = baselineCueOrder;
				break;
			case 2:
				cueOrderNow = familiarCueOrder;
				break;
			default:
				for (int i = 0; i < 5; i++)
				{
					cueOrderNow[i] = dayCueOrders[SessionDay, currentCueOrder - 2, i];
				}
				break;
		}
	}

	void SetObjectPositions ()
    {
		//UpdateCueOrder();

		//Debug.Log("nObjPosRel :" + objectPositionsRel.Length);
		int relObjPos = 0;
		int currentCueOrder = cueOrderList[ numTraversals ];
		int[] cueOrderNow = new int[5];
		
		switch (currentCueOrder)
		{ 
			case 1:
				cueOrderNow = baselineCueOrder;
				break;
			case 2:
				cueOrderNow = familiarCueOrder;
				break;
			default:
				for (int i = 0; i < 5; i++)
				{
                    cueOrderNow[i] = dayCueOrders[SessionDay, currentCueOrder - 2, i];
				}
				break;
		}
		for (int i = 0; i<5; i++)
        {
			//Debug.Log("Cue " + i + " slot " + cueOrderNow[i]);
        }
		
		for (int i=0; i < 5; i++)
        {
			relObjPos = cueOrderNow[i];


			/*
			switch (blockType[numTraversals])
            {
				case 1:
					relObjPos = baselineCueOrder [i];
					break;
				case 2:
					//relObjPos = remapCueOrder [i];
					break;
				case 3:
					relObjPos = baselineCueOrder[i];
					break;
			}
			*/
			
			float relZpos = objectPositionsRel[relObjPos - 1];
			float targetZpos = (float)trackLen * relZpos;
			//Debug.Log("trackLen " + (float)trackLen);
			//Debug.Log("Moving object " + i + " to relative pos " + relZpos + " realDist " + targetZpos);
			GameObject objHere = ObjectCues[i];
			Vector3 pHere = objHere.transform.localPosition;
			pHere[2] = targetZpos;
			objHere.transform.localPosition = pHere;
			//Debug.Log("Obj " + i + " realPos is " + objHere.transform.localPosition[2]);

			//Debug.Log("bt " + blockType[numTraversals] + ", obj " + i + ", relObjPos " + relObjPos);
		}
    }
	/*
	IEnumerator Reward()
	{
		if (ArdUse == ArduinoUse.Uniduino) {
		//arduino.digitalWrite(rewardPin, Arduino.HIGH);
		//yield return new WaitForSeconds(0.06f);
		//arduino.digitalWrite(rewardPin, Arduino.LOW);
		}
	}
	*/
	/*
	IEnumerator StopSession()
	{
		if (ArdUse == ArduinoUse.Uniduino)
		{
			//arduino.digitalWrite(stopPin, Arduino.HIGH);
			//yield return new WaitForSeconds(0.1f);
			//arduino.digitalWrite(stopPin, Arduino.LOW);
		}
		//StartCoroutine (SendEmail ()); // send email notification that session is done
		UnityEditor.EditorApplication.isPlaying = false;
	}			
	*/

	private void connect(string serialPortName, Int32 baudRate, bool autoStart, int delay)
	{
		_serialPort = new SerialPort(serialPortName, baudRate);
		//_serialPort = Win32SerialPort.CreateInstance();

		_serialPort.DtrEnable = true; // win32 hack to try to get DataReceived event to fire
		_serialPort.RtsEnable = true;
		_serialPort.PortName = serialPortName;
		_serialPort.BaudRate = baudRate;

		_serialPort.DataBits = 8;
		_serialPort.Parity = Parity.None;
		_serialPort.StopBits = StopBits.One;
		_serialPort.ReadTimeout = 5; // since on windows we *cannot* have a separate read thread
		_serialPort.WriteTimeout = 1000;


		if (autoStart)
		{
			this.delay = delay;
			this.Open();
		}
	}

	private void connectRwd(string serialPortName, Int32 baudRate, bool autoStart, int delay)
	{
		_serialPortReward = new SerialPort(serialPortName, baudRate);
		//_serialPort = Win32SerialPort.CreateInstance();

		_serialPortReward.DtrEnable = true; // win32 hack to try to get DataReceived event to fire
		_serialPortReward.RtsEnable = true;
		_serialPortReward.PortName = serialPortName;
		_serialPortReward.BaudRate = baudRate;

		_serialPortReward.DataBits = 8;
		_serialPortReward.Parity = Parity.None;
		_serialPortReward.StopBits = StopBits.One;
		_serialPortReward.ReadTimeout = 5; // since on windows we *cannot* have a separate read thread
		_serialPortReward.WriteTimeout = 1000;


		if (autoStart)
		{
			this.delay = delay;
			this.OpenRwd();
		}
	}

	private void Open()
	{
		_serialPort.Open();

		if (_serialPort.IsOpen)
		{
			Thread.Sleep(delay);
		}
	}

	private void OpenRwd()
	{
		_serialPortReward.Open();

		if (_serialPortReward.IsOpen)
		{
			Thread.Sleep(delay);
		}
	}

	private void Close()
	{
		if (_serialPort != null)
			_serialPort.Close();

		if (_serialPortReward != null)
			_serialPortReward.Close();
	}

	private void Disconnect()
	{
		Close();
	}

	void OnDestroy()
	{
		Disconnect();
	}

	// save trial data to server
	void OnApplicationQuit()
	{
		Debug.Log("Quitting");
		Debug.Log("Stopping 2 " + Time.realtimeSinceStartup);
		cmdWrite = 4;
		if (ArdUse == ArduinoUse.Uniduino)
		{
			_serialPort.Write(cmdWrite.ToString() + ',');
		}

		if (saveData == true)
		{
			sw_startstop.Write("StopSignalSent" + "\t" + Time.realtimeSinceStartup + "\n");

			sw_startstop.Close();
			sw_trial.Close();
			sw_pos.Close();
			sw_reward.Close();
			sw_lick.Close();
		}

		if (saveData == true)
		{
			File.Copy(trialTimesFile, serverTrialTimesFile);
			File.Copy(rewardFile, serverRewardFile);
			File.Copy(lickFile, serverLickFile);
			File.Copy(positionFile, serverPositionFile);
		}
	}

}
