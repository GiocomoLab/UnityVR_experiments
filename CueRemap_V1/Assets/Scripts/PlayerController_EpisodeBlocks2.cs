
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

public class PlayerController_EpisodeBlocks2 : MonoBehaviour
{
	// arduino
	/*
	public Arduino arduino;
	private int rewardPin = 12; // for administering rewards
	private int stopPin = 9; // for stopping recording at end of session
	*/
	private int lickThreshold = 500; // 500 default

	public string thisPort = "COM6";
	private SerialPort _serialPort;
	private int delay;
	private bool testtest;
	private int missedArdFrame = 0;

	//public string rewardPort = "COM5";
	//private SerialPort _serialPortReward;
	
	public GameObject rewardZone;

	public enum ArduinoUse { Uniduino, SimulatedRunning };
	public ArduinoUse ArdUse;
	[HideInInspector]
	public bool simulatedRunning = false;
	public float simulatedRunningSpeed = 75.0f;
	private float startOffsetRange;

	public int SessionDay = 1;
	public int CueSet = 1;

	public int lickPinValue;
	public int lickValThreshold = 500; // default 500


	private int floorLength = 400;
	private int floorTileReps = 100;
	//private Component frontCamera = GetComponentInChildren<panoCamera.subCam1>;
	private int cameraViewDist = 165;
	public int cameraDistReadOnly;

	// movement / trial tracking
	private float initialPosition_z = 0.0f; // The Start of the track
	public float startOffsetStart = -110.0f;   // Upper bound on a random starting offset
	public float startOffsetEnd = -50.0f;   // Lower bound on a random starting offset
	private Vector3 initialPosition;
	private Vector3 lastPosition;
	private Vector3 blackoutBoxPosition = new Vector3(0f, -24.0f, 526.0f);
	public int numTraversals = 0;
	private int numTrialsPerBlock;
	private float remapTrack_x = 0.0f;

	private string lick_raw;
	private int numLicks = 0;
	public int lickFlag = 0;
	private int rotaryTicks = 0;
	private float speed = 0.0447f;
	private int syncPinState;
	//private int arduinoRespondedFlag = 0;
	private float delta_z;
	private float current_z;
	private float delta_T;

	public bool useLingerForReward = false;
	public float lingerZoneStart = -10.0f; // Relative to reward zone
	public float lingerZoneDuration = 5.0f; // How long have to get there
	private float rewardLingerZoneStart;
	private float timeInLingerZone = 0.0f;

	// Reward stuff for gain manip track
	public int numGainManipTrials = 0;
	public int warmupFreeRewardsGM = 20; // How many rewards delivered automatically at the start of the session
	public int rewardMissesBeforeReminderGM = 3; // How many trials mouse can miss before automatic reminder reward
	public int minRewardsPerLapGM = 1;
	public int maxRewardsPerLapGM = 3;
	public bool requireLickForRewardGM = false;
	private int numMissedRewardsGM = 0;
	private Vector3 rewardZonePos = new Vector3(-200.0f, 0.1f, 0f);

	// Reward stuff for main track
	public int warmupFreeRewards = 20; // How many rewards delivered automatically at the start of the session
	public int rewardMissesBeforeReminder = 3; // How many trials mouse can miss before automatic reminder reward
	public bool requireLickForReward = false;
	private int numMissedRewards = 0;
	public bool newRewardPosEachBlock = false;
	private float originalRewardPos;
	public GameObject rewardObj; // This one is for visualizing reward zone

	// for each trial's reward
	public int rewardTrial;
	public float rewardPosition;
	public float familiarRewardPos;
	public float rewardZoneStart;
	public float rewardZoneEnd;
	public float rewardZoneBuffer = 25f;
	private bool lickForReward;
	private int rewardFlag = 0;
	private int automaticRewardFlag; // = 1;
	private int totalRequestedRewards = 0;
	private int totalNonAutoRewards = 0;

	// Remapping stuff
	public int numBaselineTrials = 40; // How many trials to start the session with repeatable cues order
	public int numTrialsMin = 210;     // Block sizes are random, so we need a range for final number of trials
	public int numTrialsMax = 250;
	public int minBlockSize = 20;      // Min number of trials for random blocks
	public int maxBlockSize = 35;      // Max number of trials for random blocks
	public int numBaselineRepeats = 2;  // How many times to work in the baseline cue order 
	[HideInInspector]
	public int numTrialsTotal;

	public float rewardLocRel;// = 0.6f;

	public int[] baselineCueOrder = { 1, 2, 3, 4, 5 }; // The cue order to use as a baseline, repeated multiple times each session
	//public int[] familiarCueOrder = { 3, 4, 1, 5, 2 };//{2, 4, 1, 5, 3};  The cue order to repeat once each session
	[HideInInspector]
	public int[] familiarCueOrder = new int[5];
	[HideInInspector]
	public List<int> cueOrderList = new List<int>();
	[HideInInspector]
	int[] cueOrderNow = new int[5];

	public int[,,] dayCueOrders = new int[3,8,5]; //[SessionDay, currentCueOrder - 2, i]

	[HideInInspector]
	public List<int> trialCueOrder; // Which cue order to use on each trial
	[HideInInspector]
	public int numberDayCueOrders = 10; // number of lists to draw from for each day

	public float[] objectPositionsRel;// = { 0.1f, 0.23f, 0.5f, 0.78f, 0.9f }; // This determined by  = 0.6f;
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
	public List<int> rewardCenters;

	private float rewardPositionGM;
	private float rewardZoneStartGM;
	private float rewardZoneEndGM;
	public int rewardTrialGM;
	private List<int> rewardTrialsGM;
	private List<int> rewardCentersGM;
	private List<int> automaticRewards;
	private float gainManipTrack_x = -200.0f;
	private Vector3 initialPositionGM;
	// for detecting licks
	private List<float> floorCycleStarts;
	private List<float> teleportCycleStarts;
	private float trackDistPerTileRepeat;
	
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

	public float rewardRateGainManip = 0.005f;
	public float timeoutAfterGainManip = 60.0f;
	//private float teleportPositionGM = 400.0f;
	private int trackLenGM = 400;
	private bool finishedPostGMtimeout = true;
	private float postGMtimeoutOver = 0.0f;
	private bool doneGainManipTrials = false;

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
	private string serverStartStopFile;
	private string paramsTwoFile;
	private string serverParamsTwoFile;
	private string trialListFile;
	private string serverTrialListFile;
	private string trialListFileGM;
	private string serverTrialListFileGM;
	private string blockInfoFile;
	private string serverBlockInfoFile;

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
	private StreamWriter sw_trialListGM;
	private StreamWriter sw_trialOrder;
	private StreamWriter sw_parpar;

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
		/*
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
		*/
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

			//_serialPortReward.DiscardInBuffer();
			//_serialPortReward.DiscardOutBuffer();
		}

		Debug.Log("end of wake");
	}

	void Start()
	{
		Debug.Log("Begin Start");

		cameraDistReadOnly = cameraViewDist;

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
		float familiarRewardRel = 0.0f; ;

		if (CueSet == 1)
		{
			objectPositionsRel = new float[] { 0.1f, 0.23f, 0.5f, 0.75f, 0.85f }; 
			rewardLocRel = 0.6f;
			familiarRewardRel = 0.85f;
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
		else if (CueSet == 2)
		{
			objectPositionsRel = new float[] { 0.14f, 0.29f, 0.45f, 0.62f, 0.92f};
			rewardLocRel = 0.75f;
			familiarRewardRel = 0.4f;
			familiarCueOrder = new int[] { 2, 4, 1, 5, 3 };
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
		/*
		Debug.Log("dcc1 = " + dayCueOrders[0, 1, 1]);
		Debug.Log("dcc2 = " + dayCueOrders[1, 2, 2]);
		Debug.Log("dcc3 = " + dayCueOrders[2, 3, 3]);
		Debug.Log("SessionDay = " + SessionDay);
		Debug.Log("dcc4 = " + dayCueOrders[SessionDay, 4, 4]);
		*/



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
			//Debug.Log(baseRep + " ; " + numBaselineRepeats);
			bool slotGood = false;
			for (int attemptI = 0; attemptI < 50; attemptI++)
			{
				//Debug.Log(attemptI);
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
		if (numTrialsMin > numBaselineTrials)
		{

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
		}

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
		Debug.Log("Lap " + (numTraversals + 1) + " start offset " + randOffset + ", pos " + (initialPosition_z + randOffset));

		initialPosition = new Vector3(remapTrack_x, 0.5f, initialPosition_z + randOffset);
		transform.position = initialPosition;
		
		initialPositionGM = new Vector3(gainManipTrack_x, 0.5f, initialPosition_z);

		if (numGainManipTrials > 0)
        {
			transform.position = initialPositionGM;
		}
		lastPosition = transform.position;

		// reward params
		lickForReward = paramsScript.lickForReward;

		// DEFINE REWARD SEQUENCE
		rewardCenters = new List<int>();
		blockType = new List<int>();
		rewardTrials = new List<int>();

		// Setup GainManip trials
		rewardTrialsGM = new List<int>();
		rewardCentersGM = new List<int>();
		automaticRewards = new List<int>();

		//int trial;
		//int pos;

		// This might need to be rewritten for some number expected rewards each lap
		if (numGainManipTrials > 0)
		{
			for (int i = 0; i < numGainManipTrials; i++)
            {
				float randThisInt = UnityEngine.Random.Range((float)minRewardsPerLapGM - 0.5f, (float)maxRewardsPerLapGM + 0.5f);
				int rewardsThisLap = (int)randThisInt;
				if (rewardsThisLap > 0)
				{
					bool doneLapRewards = false;
					int numLapRewardsTries = 0;
					int[] possRewards = new int[rewardsThisLap + 2];

					while (doneLapRewards == false)
					{
						
						possRewards[0] = 50;
						bool possRewardsGood = true;

						for (int lapReward = 0; lapReward < rewardsThisLap; lapReward++)
						{
							// Generate random reward locations
							// get three ints, if fit conditions below then good, otherwise try again?
							possRewards[lapReward+1] = UnityEngine.Random.Range(possRewards[lapReward], trackLenGM - 50);

							if ((possRewards[lapReward+1] - possRewards[lapReward]) < 50)
							{
								possRewardsGood = false;
							}
						}

						numLapRewardsTries++;
						doneLapRewards = possRewardsGood;
					}

					for (int prI = 0; prI < rewardsThisLap; prI++)
                    {
						rewardCentersGM.Add(possRewards[prI + 1]);
						rewardTrialsGM.Add(i);
						if (rewardCentersGM.Count < warmupFreeRewardsGM+1)
                        {
							automaticRewards.Add(1);
                        }
                        else
                        {
							automaticRewards.Add(0);
						}
                    }
				}
			}

			/*
			for (int i = 0; i < ((numGainManipTrials - 1) * trackLenGM); i++)
			{
				float thresh = UnityEngine.Random.value;

				if (i == 0)
                {
					Debug.Log("thresh" + thresh);
                }
				// give rewards with probability defined by pReward and track length
				// exclude first and last 50cm of track
				if (thresh <= rewardRateGainManip & i % trackLenGM >= 50 & i % trackLenGM <= trackLenGM - 50)
				{
					trial = i / trackLenGM;
					pos = i % trackLenGM;

					// first reward
					if (rewardCentersGM.Count == 0)
					{
						rewardCentersGM.Add(pos);
						rewardTrialsGM.Add(trial);
						if (requireLickForReward == false)
						{
							automaticRewards.Add(1);
						}
						else
						{
							if (warmupFreeRewardsGM > 0)
							{
								automaticRewards.Add(1);
							}
							else
							{
								automaticRewards.Add(0);
							}
						}
					}

					// following rewards must be at least 50cm away
					// same trial:
					if ((trial == rewardTrialsGM[rewardTrialsGM.Count - 1]) & (pos < (rewardCentersGM[rewardCentersGM.Count - 1] + 50)))
					{
						continue;
					}
					// next trial but still within 50cm
					else if ((trial > rewardTrialsGM[rewardTrialsGM.Count - 1]) & ((pos + 400) < (rewardCentersGM[rewardCentersGM.Count - 1] + 50)))
					{
						continue;
					}
					// passes criteria
					else
					{
						rewardCentersGM.Add(pos);
						rewardTrialsGM.Add(trial);

						if (requireLickForRewardGM == true)
						{
							if (rewardCenterGMs.Count > warmupFreeRewardsGM)
							{
								automaticRewards.Add(0);
							}
							else
							{
								automaticRewards.Add(1);
							}
						}
						else
						{
							automaticRewards.Add(1);
						}
					}
				}
			}
			*/

			// initialize reward zone (if any rewards)
			if (rewardCentersGM.Count > 0)
			{
				rewardTrialGM = rewardTrialsGM[0];
				rewardPositionGM = rewardCentersGM[0];
				rewardZoneStartGM = rewardCentersGM[0] - 25;
				rewardZoneEndGM = rewardCentersGM[0] + 25;
				automaticRewardFlag = automaticRewards[0];
			}
			Debug.Log("Made a list of reward locations/trials for the " + numGainManipTrials + " gainmanip track trials.");
			Debug.Log("First reward trial " + rewardTrialGM + ", position " + rewardPositionGM + ", automatic is " + automaticRewardFlag);

			numTrialsTotal = numTrialsTotal + numGainManipTrials;
		}
		else
		{
			Debug.Log("There are " + numGainManipTrials + " so we are skipping that section.");
		}

		rewardZonePos[2] = (float)rewardPositionGM;
		rewardZone.transform.localPosition = rewardZonePos;

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
		try 
		{
			Debug.Log("1");
			Vector3 rwObjPos = rewardObj.transform.position;
			Debug.Log("2");
			rwObjPos[2] = rewardPosition;
			Debug.Log("3");
			rewardObj.transform.localPosition = rwObjPos;
            Debug.Log("Set reward marker position at " + rewardPosition);
		} 
		catch { Debug.Log("Failed to set reward visualizer"); }

		familiarRewardPos = trackLen * familiarRewardRel;
		originalRewardPos = rewardPosition;
		rewardZoneStart = rewardPosition - rewardZoneBuffer;
		rewardZoneEnd = rewardPosition + rewardZoneBuffer;
		rewardLingerZoneStart = rewardZoneStart + lingerZoneStart;
		doneGainManipTrials = true;
		if (numGainManipTrials > 0)
        {
			rewardTrial = numGainManipTrials + 1;
			doneGainManipTrials = false;
        }
			
		if (timeoutAfterGainManip > 0)
        {
			finishedPostGMtimeout = false;
			if (numGainManipTrials == 0)
            {
				Debug.Log("Starting in blackbox");
				transform.position = blackoutBoxPosition;
            }
        }
		// for saving data	
		mouse = paramsScript.mouse;
		session = paramsScript.session;
		saveData = paramsScript.saveData;
		
		if (saveData == true)
		{
			localDirectory = paramsScript.localDirectory;
			serverDirectory = paramsScript.serverDirectory;

			string localDirectoryMouse = localDirectory + "\\" + mouse;
			string serverDirectoryMouse = serverDirectory + "\\" + mouse;
			if (!Directory.Exists(localDirectoryMouse))
            {
				// Quit
				Debug.Log("Local mouse directory does not exist");
				UnityEditor.EditorApplication.isPlaying = false;
			}
			if (!Directory.Exists(serverDirectoryMouse))
            {
				Debug.Log("No server directory for this mouse, will not copy over");
            }

			trialTimesFile = localDirectoryMouse + "\\" + session + "_trial_times.txt";
			serverTrialTimesFile = serverDirectoryMouse + "\\" + session + "_trial_times.txt";
			rewardFile = localDirectoryMouse + "\\" + session + "_reward.txt";
			serverRewardFile = serverDirectoryMouse + "\\" + session + "_reward.txt";
			positionFile = localDirectoryMouse + "\\" + session + "_position.txt";
			serverPositionFile = serverDirectoryMouse + "\\" + session + "_position.txt";
			lickFile = localDirectory + "\\" + mouse + "\\" + session + "_licks.txt";
			serverLickFile = serverDirectoryMouse + "\\" + session + "_licks.txt";
			startStopFile = localDirectoryMouse + "\\" + session + "_startStop.txt";
			serverStartStopFile = serverDirectoryMouse + "\\" + session + "_startStop.txt";
			paramsTwoFile = localDirectoryMouse + "\\" + session + "_params2.txt";
			serverParamsTwoFile = serverDirectoryMouse + "\\" + session + "_params2.txt";
			trialListFile = localDirectoryMouse + "\\" + session + "_trialList.txt";
			serverTrialListFile = serverDirectoryMouse + "\\" + session + "_trialList.txt";
			trialListFileGM = localDirectoryMouse + "\\" + session + "_trialListGM.txt";
			serverTrialListFileGM = serverDirectoryMouse + "\\" + session + "_trialListGM.txt";
			blockInfoFile = localDirectoryMouse + "\\" + session + "_blockInfo.txt";
			serverBlockInfoFile = serverDirectoryMouse + "\\" + session + "_blockInfo.txt";

			sw_lick = new StreamWriter(lickFile, true);
			sw_pos = new StreamWriter(positionFile, true);
			sw_trial = new StreamWriter(trialTimesFile, true);
			sw_reward = new StreamWriter(rewardFile, true);
			sw_startstop = new StreamWriter(startStopFile, true);
			sw_par = new StreamWriter(blockInfoFile, true);
			sw_trialList = new StreamWriter(trialListFile, true);
			sw_trialListGM = new StreamWriter(trialListFileGM, true);
			sw_parpar = new StreamWriter(paramsTwoFile, true);

			// Save some params
			sw_parpar.Write("trackLen" + "\t" + trackLen + "\n");
			sw_parpar.Write("rewardLocRel" + "\t" + rewardLocRel + "\n");
			sw_parpar.Write("rewardPosition" + "\t" + rewardPosition + "\n");
			sw_parpar.Write("rewardZoneBuffer" + "\t" + rewardZoneBuffer + "\n");
			sw_parpar.Write("warmupFreeRewards" + "\t" + warmupFreeRewards + "\n");
			sw_parpar.Write("rewardMissesBeforeReminder" + "\t" + rewardMissesBeforeReminder + "\n");
			//sw_par.Write("rewardRemindersAfterMiss" + "\t" + rewardRemindersAfterMiss + "\n");
			sw_parpar.Write("requireLickForReward" + "\t" + requireLickForReward + "\n");
			sw_parpar.Write("blackoutBoxPosition" + "\t" + blackoutBoxPosition + "\n");
			sw_parpar.Write("UseTimeout" + "\t" + UseTimeout + "\n");
			sw_parpar.Write("TimeoutMinimum" + "\t" + TimeoutMinimum + "\n");
			sw_parpar.Write("TimeoutMaximum" + "\t" + TimeoutMaximum + "\n");
			sw_parpar.Write("BlackRunningSeconds" + "\t" + BlackRunningSeconds + "\n");
			sw_parpar.Write("startOffsetStart" + "\t" + startOffsetStart + "\n");
			sw_parpar.Write("startOffsetEnd" + "\t" + startOffsetEnd + "\n");
			sw_parpar.Write("numTrialsMin" + "\t" + numTrialsMin + "\n");
			sw_parpar.Write("numTrialsMax" + "\t" + numTrialsMax + "\n");
			sw_parpar.Write("minBlockSize" + "\t" + minBlockSize + "\n");
			sw_parpar.Write("maxBlockSize" + "\t" + maxBlockSize + "\n");
			sw_parpar.Write("numGainManipTrials" + "\t" + numGainManipTrials + "\n");
			sw_parpar.Write("lickThreshold" + "\t" + lickThreshold + "\n");
			sw_parpar.Write("CueSet" + "\t" + CueSet + "\n");
			sw_parpar.Write("SessionDay" + "\t" + SessionDay + "\n");
			sw_parpar.Write("cameraDistReadOnly" + "\t" + cameraDistReadOnly + "\n");

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

			if (numGainManipTrials > 0)
			{
                Debug.Log("num reward centers = " + rewardCentersGM.Count + ", num reward trials = " + rewardTrialsGM.Count + ", num automatic rewards = " + automaticRewards.Count);

				int numRewardsGM = rewardCentersGM.Count;
				for (int ttI = 0; ttI < numRewardsGM; ttI++)
				{
					sw_trialListGM.Write("trial " + ttI + "\t" + rewardTrialsGM[ttI] + "\t" + rewardCentersGM[ttI] + "\t" + automaticRewards[ttI] + "\n");
				}
				sw_trialListGM.Close();
			}

		}
		else
        {
			//Debug.Log("Warning: this is NOT saving any data");
        }


		// Setup seamless teleport stuff
		int scaledTrackLen = floorLength * 10;
		trackDistPerTileRepeat = (float)scaledTrackLen / (float)floorTileReps;

		float trackStart = -1 * (float)scaledTrackLen / 2;
		float trackEnd = (float)scaledTrackLen / 2;
		
		floorCycleStarts = new List<float>();
		float currCycleStart = trackStart;
		while (currCycleStart <= trackEnd)
        {
			floorCycleStarts.Add(currCycleStart); // These are all the locations along the track where the optic flow shader repeats
			currCycleStart += trackDistPerTileRepeat;
		}
		Debug.Log("floorCycleStarts start/end = " + floorCycleStarts[0] + ", " + floorCycleStarts[floorCycleStarts.Count-1]);

		// Find share repeats within teleport range
		float minTeleport = -1 * ( ((float)cameraViewDist) + 20.0f + (float)trackDistPerTileRepeat );
		float maxTeleport = startOffsetStart;
		startOffsetEnd = startOffsetEnd - trackDistPerTileRepeat; // Need to allow a full cycle for seamless teleportation
		// Accomodate user-set boundaries
		if (minTeleport > startOffsetEnd) { minTeleport = startOffsetEnd; }
		if (trackStart > maxTeleport) { maxTeleport = trackStart; }

		// Find the cycles within boundaries
		//int floorCyclesWithinBounds = 0;
		teleportCycleStarts = new List<float>();
		Debug.Log("minTeleport = " + minTeleport + ", maxTeleport = " + maxTeleport + ", trackDistPerCycleRepeat = " + trackDistPerTileRepeat);
		for (int i=0; i<floorCycleStarts.Count; i++)
        {
			//Debug.Log("int i = " + i + " start " + floorCycleStarts[i]);
			if (floorCycleStarts[i] >= maxTeleport && floorCycleStarts[i] <= minTeleport)
            {
				teleportCycleStarts.Add(floorCycleStarts[i]);
				//Debug.Log("int i = " + i + " teleport i = " + teleportCycleStarts.Count + ", start " + teleportCycleStarts[teleportCycleStarts.Count-1]);
			}
		}
		Debug.Log("Came up with " + teleportCycleStarts.Count + " teleportCycleStarts, from " + teleportCycleStarts[0] + " to " + teleportCycleStarts[teleportCycleStarts.Count-1]);
		//float tpTest = teleportCycleStarts[0];
		//Debug.Log(tpTest);

		/*


		float minTeleport = ((float)cameraViewDist + 50.0f);
		float maxTeleport = (float)scaledTrackLen / 2;
		int teleportRange = -1 * ((int)maxTeleport - (int)minTeleport); // 
		//Debug.Log("teleport range = " + teleportRange);
		//Debug.Log(" n teleport cycles = " + (-1*teleportRange / trackDistPerTileRepeat));
		//int[] floorCycleStarts = new int[(-1 * teleportRange / trackDistPerTileRepeat) + 1];
		
		int currCycleStart = (int)maxTeleport * -1;
		//Debug.Log("currCycleStart = " + currCycleStart);
		
		int numCycleOptions = 0;
		while (currCycleStart <= -1 * (int)minTeleport)
		{
			Debug.Log("numCycleOptions, currcycleStart " + numCycleOptions + ", " + currCycleStart);
			floorCycleStarts[numCycleOptions] = currCycleStart;
			numCycleOptions++;
			currCycleStart += trackDistPerTileRepeat;
		}

		int randCycleOffset = UnityEngine.Random.Range(1, numCycleOptions);
		float teleportOffset = (float)floorCycleStarts[randCycleOffset - 1] + withinTileOffset;
		Debug.Log("Lap " + (numTraversals + 1) + " cycle offset " + randCycleOffset + ", pos " + (float)floorCycleStarts[randCycleOffset - 1] + ", actual pos " + teleportOffset);
		*/
		Debug.Log("Press return to start the session");

		if (Application.targetFrameRate != target)
			Application.targetFrameRate = target;
	}

	void Update()
	{
		cameraDistReadOnly = cameraViewDist;

		//missedArdFrame = false;
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

			cmdWrite = 1;
			//int missedArdFrame = 0;
			if (ArdUse == ArduinoUse.Uniduino)
			{
				_serialPort.Write(cmdWrite.ToString() + ',');
				//int missedArdFrame = 0;
				try
				{
					lick_raw = _serialPort.ReadLine();

					if (lick_raw[0] == 0)
					{
						Debug.Log("Bad read");
						lick_raw = string.Concat("1", lick_raw);
					}
					string[] lick_list = lick_raw.Split('\t');

					if (lick_list.Length < 4)
					{
						Debug.Log("Bad convert, setting lick val at 1023");
						//Debug.Log(lick_raw);
						//Debug.Log(lick_list);
						if (lick_list.Length == 3)
						{
							lick_list[3] = lick_list[2];
							lick_list[2] = lick_list[1];
							lick_list[1] = lick_list[0];
							lick_list[0] = "1023";
						}
					}

					lickPinValue = int.Parse(lick_list[0]);
					// Aruino output should only end in 3 if it was supposed to be 1023
					if ((lickPinValue % 10) == 3)
					{
						lickPinValue = 1023;
					}


					//if (lickPinValue > 500 & lickFlag == 1)
					if (lickPinValue > lickValThreshold)
					{
						lickFlag = 0;
					}
					//if (lickPinValue < 500 & lickFlag == 0)
					if (lickPinValue <= lickValThreshold)
					{
						numLicks += 1;
						lickFlag = 1;
					}
				}
				catch (TimeoutException)
				{
					Debug.Log("lickport/encoder timeout on frame " + Time.frameCount);
					//missedArdFrame = 1;
				}
				catch
				{
					Debug.Log("cmd " + cmdWrite + " reply " + lick_raw + " frame " + Time.frameCount);
					//missedArdFrame = 1;
				}
			}

			if (ArdUse == ArduinoUse.Uniduino)
			{
				_serialPort.DiscardInBuffer();
				_serialPort.DiscardOutBuffer();
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

				/*
				if (ArdUse == ArduinoUse.Uniduino)
				{
					_serialPortReward.DiscardInBuffer();
					_serialPortReward.DiscardOutBuffer();
				}
				*/
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

			//if (numGainManipTrials > 0) 
            //{
				if (finishedPostGMtimeout == false)
				{
					if (numTraversals == numGainManipTrials)
					{
						float timeNow = Time.realtimeSinceStartup;

						if (timeNow > postGMtimeoutOver)
						{
							Debug.Log("timeout ended at " + postGMtimeoutOver + "; time now is " + timeNow);
							finishedPostGMtimeout = true;
						}
						else
						{
							finishedPostGMtimeout = false;
						}

						if (finishedPostGMtimeout == false)
						{
							transform.position = blackoutBoxPosition;
							allowMovement = false;
						}
						else
						{
							transform.position = initialPosition;
							lastPosition = transform.position;
							Debug.Log("sending to x pos " + initialPosition[0]);
							Debug.Log("sending to z pos " + initialPosition[2]);
							allowMovement = true;
							doneGainManipTrials = true;
							Debug.Log("ending post GM timeout");
							SetObjectPositions();
						}
					}
				}
			//}

			

			if (doneGainManipTrials == true)
			{
				// reset teleport and reward flags in appropriate zone
				rewardZoneStart = rewardPosition - rewardZoneBuffer;
				rewardZoneEnd = rewardPosition + rewardZoneBuffer;
				if (transform.position.z < (rewardZoneStart - 20))
				{
					rewardFlag = 0;
				}
				if (transform.position.z < rewardZoneStart - 20)
				{
					//teleportFlag = 0;
				}
				if (requireLickForReward == true)
				{
					if (numTraversals < numGainManipTrials + warmupFreeRewards)
					{
						automaticRewardFlag = 1;
					}
					else
					{
						automaticRewardFlag = 0;
					}

					if (numMissedRewards >= rewardMissesBeforeReminder)
                    {
						automaticRewardFlag = 1;
                    }
				}



				// Don't need to reset missedRewards counter, kept separately for gain manip and cue remap

				// reward
				// always allow animal to lick for reward in zone if box checked
				/*
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
				*/

				// reward

				//Debug.Log(rewardFlag);
				// always allow animal to lick for reward in zone if box checked
				if (useLingerForReward == true)
				{
					if (rewardFlag == 0 & transform.position.z > rewardLingerZoneStart & transform.position.z < rewardZoneEnd)
					{
						if (lastPosition[2] > rewardLingerZoneStart & lastPosition[2] < rewardZoneEnd)
						{
							timeInLingerZone = timeInLingerZone + Time.deltaTime;
							if (timeInLingerZone > lingerZoneDuration & lickFlag == 0)
							{
								rewardFlag = 1;
								cmdWrite = 2;
								totalNonAutoRewards++;

								numMissedRewards = 0;
								rewardCount += 1;
								Debug.Log("Reward for linger time, linger time" + timeInLingerZone + " Trial= " + (numTraversals + 1));
								Debug.Log("Requested " + totalRequestedRewards + " out of " + totalNonAutoRewards);
								timeInLingerZone = 0.0f;
								if (saveData)
								{
									sw_reward.Write(Time.realtimeSinceStartup + "\t" + rewardTrial + "\t" + rewardPosition + "\t" + numTraversals + "\t" + 0 + "\t" + 0 + "\n");
								}
							}
						}
					}
				}

				if (lickForReward & transform.position.z > rewardZoneStart & transform.position.z < rewardZoneEnd & rewardFlag == 0)
				{
					if (lickFlag == 1)
					{
						rewardFlag = 1;
						//StartCoroutine (Reward ());
						cmdWrite = 2;
						totalRequestedRewards++;
						totalNonAutoRewards++;

						numMissedRewards = 0;
						rewardCount += 1;
						// update debug log and save info
						Debug.Log(lick_raw + "; " + lickFlag);
						Debug.Log("Reward requested " + " Trial= " + (numTraversals + 1) + " lickPinVal= " + lickPinValue  + ", automaticReward= " + automaticRewardFlag + ", num missed rewards= " + numMissedRewards);

						Debug.Log("Requested " + totalRequestedRewards + " out of " + totalNonAutoRewards);
						if (saveData)
						{
							//var sw = new StreamWriter(rewardFile, true);
							sw_reward.Write(Time.realtimeSinceStartup + "\t" + rewardTrial + "\t" + rewardPosition + "\t" + numTraversals + "\t" + 0 + "\t" + 0 + "\n");
							//sw_reward.Write(rewardCount + "\t" + numTraversals + "\t" + rewardPosition + "\t" + "Missed")
							//sw.Close();
						}

					}
				}

				// automatic reward
				if (automaticRewardFlag == 1 & transform.position.z > rewardPosition & transform.position.z < rewardZoneEnd & rewardFlag == 0)
				{
					rewardFlag = 1;
					//StartCoroutine (Reward ());
					cmdWrite = 2;
					numMissedRewards = 0;

					rewardCount += 1;
					// update debug log
					Debug.Log("Auto reward delivery " + "Trial= " + (numTraversals + 1)  + ", reward pos= " + rewardPosition + ", automaticReward= " + automaticRewardFlag + ", num missed rewards= " + numMissedRewards);

					if (saveData)
					{
						//var sw = new StreamWriter(rewardFile, true);
						sw_reward.Write(Time.realtimeSinceStartup + "\t" + rewardTrial + "\t" + rewardPosition + "\t" + numTraversals + "\t" + 1 + "\t" + 0 + "\n");
						//sw.Close();
					}

				}

				// missed reward
				if (automaticRewardFlag == 0 & transform.position.z > rewardZoneEnd & rewardFlag == 0)
				{
					rewardFlag = 1;
					numMissedRewards = numMissedRewards + 1;
					totalNonAutoRewards++;

					// update debug log 
					Debug.Log("Missed Reward, Trial= " + (numTraversals + 1) + " lickPinVal= " + lickPinValue + ", automaticReward= " + automaticRewardFlag + ", num missed rewards= " + numMissedRewards);
					sw_reward.Write(Time.realtimeSinceStartup + "\t" + rewardCount + "\t" + rewardPosition + "\t" + numTraversals + "\t" + 0 + "\t" + 1 + "\n");
					Debug.Log("Requested " + totalRequestedRewards + " out of " + totalNonAutoRewards);

					if (numMissedRewards >= rewardMissesBeforeReminder)
					{
						automaticRewardFlag = 1;
						//numMissedRewards = 0;
						Debug.Log("Next trial automatic reward");
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
								if (TimeoutMaximum <= 0.0f)
                                {
									timeoutDelay = -1.0f;
                                }
							}
							timeoutOver = Time.realtimeSinceStartup + timeoutDelay;
							haveSetTimeoutEnd = true;
							Debug.Log("Time out for " + timeoutDelay + "s" + "; This was at " + System.DateTime.Now);
							//Debug.Log(Time.realtimeSinceStartup + ", " + timeoutOver);
							try
							{
								//Debug.Log("This was at " + System.DateTime.Now);
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
							//Debug.Log("Done timeout");
						}
					}

					if (allowLapEnd == true)
					{
						// update number of trials
						numTraversals += 1;
						// At the end of the last lap, this causes it to tick up to num trials total, which with 0-intexing is one more than the listed number of trials
						Debug.Log("Ending lap: numTraversals = " + numTraversals + " / numTrialsTotal = " + numTrialsTotal);
						//teleportFlag = 1;

						// set automaticRewardFlag to 1 if the upcoming trial will be automatic reward
						//automaticRewardFlag = 1;

						// TELEPORT
						// Original
						/*
						float randOffset = UnityEngine.Random.Range(startOffsetStart, startOffsetEnd);
						Debug.Log("Lap " + (numTraversals + 1) + " start offset " + randOffset + ", pos " + (initialPosition_z + randOffset));

						initialPosition[2] = initialPosition_z + randOffset;

						transform.position = initialPosition;

						lastPosition = transform.position;
						justTeleported = true;
						*/

						// Seamless telepoprt with floor tiles	
						//Debug.Log("1");
						float currentPosition = transform.position.z;
						//Debug.Log("currentPosition = " + currentPosition);
						float withinTileOffset = currentPosition % (float)trackDistPerTileRepeat;
						//Debug.Log("witinTileOffset = " + withinTileOffset + ", trackDistPerTileRepeat = " + trackDistPerTileRepeat);
						int randCycleOffset = UnityEngine.Random.Range(1, teleportCycleStarts.Count)-1; // Zero indexing is dumb
						//Debug.Log("randCycleOffset = " + randCycleOffset);
						float teleportOffset = teleportCycleStarts[randCycleOffset] + withinTileOffset;
						//Debug.Log("Lap = " + (numTraversals + 1) + " cycle offset index = " + randCycleOffset + ", cycle pos " + teleportCycleStarts[randCycleOffset] + ", cycle pos with current offset = " + teleportOffset);

						initialPosition[2] = teleportOffset;
						
						if (numTraversals < numTrialsTotal) // Don't teleport on last sample
						{
							transform.position = initialPosition;
							lastPosition = transform.position;
							justTeleported = true;
						}

						rewardFlag = 0;
						//Debug.Log("set reward flag to 0");

						if (saveData)
						{
							//var sw = new StreamWriter(trialTimesFile, true);
							sw_trial.Write(Time.realtimeSinceStartup + "\t" + automaticRewardFlag + "\t" + numTraversals + "\n"); //"\t" + blockType[numTraversals] + 
						}

						// write trial time to file
						if (numTraversals < numTrialsTotal)
						{
							// Update cue object positions
							SetObjectPositions();

							// update contrast and gain
							gain = 1;

							// update debug log
							//Debug.Log("Trial " + (numTraversals + 1) + ", gain = " + gain + ", reward position = " + rewardPosition);
							if (newRewardPosEachBlock == true)
							{
								int currentCueOrder = cueOrderList[numTraversals - numGainManipTrials];
								switch (currentCueOrder)
								{
									case 1:
										rewardPosition = originalRewardPos;
										break;
									case 2:
										rewardPosition = familiarRewardPos;
										break;
									default:
										if (currentCueOrder != cueOrderList[numTraversals - numGainManipTrials - 1])
										{
											bool haveRwdPos = false;
											while (haveRwdPos == false)
											{
												float rewardPosMaybe = UnityEngine.Random.Range(rewardZoneBuffer * 1.2f, trackLen - rewardZoneBuffer * 1.2f);
												if (Math.Abs(rewardPosMaybe - rewardPosition) > rewardZoneBuffer)
												{
													haveRwdPos = true;
													rewardPosition = rewardPosMaybe;
													Debug.Log("Set new reward for this block of trials at: " + rewardPosition);
												}
											}
										}
										break;
								}
								try
								{
									Vector3 rwObjPos = rewardObj.transform.position;
									rwObjPos[2] = rewardPosition;
									rewardObj.transform.localPosition = rwObjPos;
								}
								catch { }
								rewardZoneStart = rewardPosition - rewardZoneBuffer;
								rewardZoneEnd = rewardPosition + rewardZoneBuffer;
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
							//Debug.Log("trigger stop session = " + triggerStopSession);
							numTraversals = numTraversals - 1; // For better marking position file at end of session
						}

					}
				}
			}
			
			if ((numTraversals < numGainManipTrials) & (numGainManipTrials > 0))
			{
				allowMovement = true;

				rewardZonePos = rewardZone.transform.localPosition;
				rewardZonePos[2] = rewardPositionGM;

				if (numTraversals + 1 == rewardTrialGM) // Only if it's the next trial
				{
					rewardZonePos[2] = rewardPositionGM + (float)trackLenGM;
					//Debug.Log(rewardZonePos);
				}
				if (numTraversals + 1 < rewardTrialGM) // Otherwise hide it
				{
					rewardZonePos[2] = rewardPositionGM - (float)trackLenGM;
				}
				rewardZone.transform.localPosition = rewardZonePos;

				if (rotaryTicks == 0)
				{
					transform.position = lastPosition;
				}
				else
				{
					delta_z = rotaryTicks * speed;
					delta_T = Time.deltaTime;
					current_z = transform.position.z;
					transform.position = new Vector3(-200.0f, 0.5f, current_z + delta_z);
				}
				lastPosition = transform.position;

				// reset teleport and reward flags in appropriate zone
				int rewardFlagLast = rewardFlag;
				if (rewardTrialGM == numTraversals & transform.position.z < rewardZoneStart)// - 20
				{
					rewardFlag = 0;
				}

				if (rewardFlagLast != rewardFlag)
				{
					//Debug.Log("setting RF");
				}

				if (transform.position.z < rewardZoneStart - 20)
				{
					//teleportFlag = 0;
				}
				// reward

				//Debug.Log(rewardFlag);
				// always allow animal to lick for reward in zone if box checked
				if (lickForReward & rewardTrialGM == numTraversals & transform.position.z > rewardZoneStart & transform.position.z < rewardZoneEnd & rewardFlag == 0)
				{
					if (lickFlag == 1)
					{
						rewardFlag = 1;
						//StartCoroutine (Reward ());
						cmdWrite = 2;

						numMissedRewardsGM = 0;
						rewardCount += 1;
						// update debug log and save info
						Debug.Log(lick_raw + "; " + lickFlag);
						Debug.Log("Reward requested " + " Trial= " + (numTraversals + 1) + " lickPinVal= " + lickPinValue + ", rewardCount= " + rewardCount + ", rewardTrial " + rewardTrialGM + ", reward pos=" + rewardPositionGM + ", automaticReward= " + automaticRewardFlag + ", num missed rewards= " + numMissedRewardsGM);

						if (saveData)
						{
							//var sw = new StreamWriter(rewardFile, true);
							//sw_reward.Write(Time.realtimeSinceStartup + "\t" + rewardTrialGM + "\t" + rewardPositionGM + "\t" + 0 + "\t" + 0 + "\n");
							sw_reward.Write(Time.realtimeSinceStartup + "\t" + rewardCount + "\t" + rewardPosition + "\t" + numTraversals + "\t" + 0 + "\t" + 0 + "\n");
							//sw.Close();
						}
						
						// update reward zone
						// rewardCount += 1;
						if (rewardCount < rewardCentersGM.Count)
						{
							rewardTrialGM = rewardTrialsGM[rewardCount];
							rewardPositionGM = rewardCentersGM[rewardCount];
							rewardZoneStart = rewardPositionGM - 25;
							rewardZoneEnd = rewardPositionGM + 25;
							automaticRewardFlag = automaticRewards[rewardCount];
							//Debug.Log("rewardCount = " + rewardCount + ", rewardTrial= " + rewardTrialGM + ", reward pos = " + rewardPositionGM);
						}
						else
						{ // no more rewards, set zone outside track
							rewardTrialGM = numTrialsTotal + 1;
							rewardPositionGM = 800;
							rewardZoneStart = rewardPositionGM - 25;
							rewardZoneEnd = rewardPositionGM + 25;
						}


					}
				}
				
				// automatic reward
				if (automaticRewardFlag == 1 & rewardTrialGM == numTraversals & transform.position.z > rewardPositionGM & transform.position.z < rewardZoneEnd & rewardFlag == 0)
				{
					rewardFlag = 1;
					//StartCoroutine (Reward ());
					cmdWrite = 2;
					numMissedRewardsGM = 0;

					rewardCount += 1;
					// update debug log
					Debug.Log("Auto reward delivery " + "Trial= " + (numTraversals + 1) + ", rewardCount= " + rewardCount + ", rewardTrial " + rewardTrialGM + ", reward pos= " + rewardPositionGM + ", automaticReward= " + automaticRewardFlag + ", num missed rewards= " + numMissedRewardsGM);

					if (saveData)
					{
						//var sw = new StreamWriter(rewardFile, true);
						//sw_reward.Write(Time.realtimeSinceStartup + "\t" + rewardTrialGM + "\t" + rewardPositionGM + "\t" + 1 + "\t" + 0 + "\n");
						sw_reward.Write(Time.realtimeSinceStartup + "\t" + rewardCount + "\t" + rewardPosition + "\t" + numTraversals + "\t" + 1 + "\t" + 0 + "\n");
						//sw.Close();
					}

					// update reward zone
					//rewardCount += 1;
					if (rewardCount < rewardCentersGM.Count)
					{
						rewardTrialGM = rewardTrialsGM[rewardCount];
						rewardPositionGM = rewardCentersGM[rewardCount];
						rewardZoneStart = rewardPositionGM - 25;
						rewardZoneEnd = rewardPositionGM + 25;
						automaticRewardFlag = automaticRewards[rewardCount];
						//Debug.Log("rewardCount = " + rewardCount + ", rewardTrial= " + rewardTrialGM + ", reward pos = " + rewardPositionGM);
					}
					else
					{
						rewardTrialGM = numTrialsTotal + 1;
						rewardPositionGM = 800;
						rewardZoneStart = rewardPositionGM - 25;
						rewardZoneEnd = rewardPositionGM + 25;
					}
				}

				// missed reward
				if (automaticRewardFlag == 0 & rewardTrialGM == numTraversals & transform.position.z > rewardZoneEnd & rewardFlag == 0)
				{
					numMissedRewardsGM++;
					Debug.Log("Missed Reward, num traversals = " + (numTraversals) + " lickPinVal= " + lickPinValue + ", rewardCount= " + rewardCount + ", rewardTrial " + rewardTrialGM + ", reward pos= " + rewardPositionGM + ", automaticReward= " + automaticRewardFlag + ", num missed rewards= " + numMissedRewardsGM);
					
					rewardCount++;

					// update debug log 
					
					// update reward zone
					//rewardCount += 1;
					rewardFlag = 1;
					if (rewardCount < rewardCentersGM.Count)
					{
						rewardTrialGM = rewardTrialsGM[rewardCount];
						rewardPositionGM = rewardCentersGM[rewardCount];
						rewardZoneStart = rewardPositionGM - 25;
						rewardZoneEnd = rewardPositionGM + 25;
						automaticRewardFlag = automaticRewards[rewardCount];
						//Debug.Log("num traversals " + numTraversals + ", rewardCount = " + rewardCount + ", rewardTrial= " + rewardTrial + ", reward pos = " + rewardPosition);
					}
					else
					{
						rewardTrialGM = numTrialsTotal + 1;
						rewardPositionGM = 800;
						rewardZoneStart = rewardPositionGM - 25;
						rewardZoneEnd = rewardPositionGM + 25;
					}
					//Debug.Log("rewardCount = " + rewardCount + ", rewardTrial= " + rewardTrialGM + ", reward pos = " + rewardPositionGM);

					if (saveData)
					{
						//var sw = new StreamWriter(rewardFile, true);
						//sw_reward.Write(Time.realtimeSinceStartup + "\t" + rewardTrialGM + "\t" + rewardPositionGM + "\t" + 1 + "\t" + 0 + "\n");
						sw_reward.Write(Time.realtimeSinceStartup + "\t" + rewardCount + "\t" + rewardPosition + "\t" + numTraversals + "\t" + 0 + "\t" + 1 + "\n");
						//sw.Close();

					}
					if (numMissedRewardsGM >= rewardMissesBeforeReminderGM)
					{
						automaticRewardFlag = 1;
						numMissedRewardsGM = 0;
						Debug.Log("Next reward automatic");
					}
					//Debug.Log("rewardCount  " + rewardCount + " automaticRewardFlag  " + automaticRewardFlag);
				}

				if (numTraversals > rewardTrialGM)
                {
					Debug.Log("Reward count error: numTraversals = " + numTraversals + ", rewardTrial = " + rewardTrialGM + ", rewardCount = " + rewardCount + ", setting reward trial to next ahead");
					while (rewardTrialGM <= numTraversals && rewardCount <= rewardCentersGM.Count)
                    {
						rewardCount++;
						rewardTrialGM = rewardTrialsGM[rewardCount];
					}
					if (rewardCount >= rewardCentersGM.Count)
					{
						Debug.Log("Reward count = " + rewardCount + ", num reward trials GM = " + rewardTrialsGM.Count + "no more reward GM trials?");
					}
					else
					{
						rewardPositionGM = rewardCentersGM[rewardCount];
						rewardZoneStart = rewardPositionGM - 25;
						rewardZoneEnd = rewardPositionGM + 25;
						automaticRewardFlag = 1;
						Debug.Log("Reward count = " + rewardCount + ", rewardTrial = " + rewardTrialGM + " rewardPosGM = " + rewardPositionGM);
					}
                }

				// teleport
				if (transform.position.z > 400.0f)
                {
					numTraversals += 1;
					//teleportFlag = 1;
					reminderFlag -= 1;
					Debug.Log("Incremented num traversals to " + numTraversals);

					if (numTraversals == numGainManipTrials)
                    {
						Debug.Log("should be ending gainmanip trials");
						float timeNow = Time.realtimeSinceStartup;
						postGMtimeoutOver = timeNow + timeoutAfterGainManip;
						Debug.Log("timeNow is " + timeNow + "; timeout ends at " + postGMtimeoutOver);
						if (timeoutAfterGainManip > 0)
						{
							transform.position = blackoutBoxPosition;
							allowMovement = false;
							finishedPostGMtimeout = false;
							Debug.Log("Going to black box");
						}
						else
                        {
							transform.position = initialPosition;
							finishedPostGMtimeout = true;
							Debug.Log("Going to remap track");
                        }
					}
					else
                    {
						transform.position = initialPositionGM;
						Debug.Log("Teleporting to gainmanip start");
                    }

				}
				

			}

			if (ArdUse == ArduinoUse.Uniduino)
			{
				_serialPort.Write(cmdWrite.ToString() + ',');
				missedArdFrame = 0;
				try
				{
					lick_raw = _serialPort.ReadLine();

					if (lick_raw[0] == 0)
					{
						Debug.Log("Bad read");
						lick_raw = string.Concat("1", lick_raw);
					}
					string[] lick_list = lick_raw.Split('\t');

					if (lick_list.Length < 4)
					{
						Debug.Log("Bad convert, setting lick val at 1023");
						//Debug.Log(lick_raw);
						//Debug.Log(lick_list);
						if (lick_list.Length == 3)
						{
							lick_list[3] = lick_list[2];
							lick_list[2] = lick_list[1];
							lick_list[1] = lick_list[0];
							lick_list[0] = "1023";
						}
					}
					
					lickPinValue = int.Parse(lick_list[0]);
					// Aruino output should only end in 3 if it was supposed to be 1023
					if ((lickPinValue % 10) == 3)
					{
						lickPinValue = 1023;
					}


					//if (lickPinValue > 500 & lickFlag == 1)
					if (lickPinValue > lickValThreshold)
					{
						lickFlag = 0;
					}
					//if (lickPinValue < 500 & lickFlag == 0)
					if (lickPinValue < lickValThreshold)
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
					missedArdFrame = 1;
				}
				catch
				{
					Debug.Log("cmd " + cmdWrite + " reply " + lick_raw + " " + Time.frameCount);
					missedArdFrame = 1;
				}

			}
			
			if (ArdUse == ArduinoUse.Uniduino)
			{
				_serialPort.DiscardInBuffer();
				_serialPort.DiscardOutBuffer();

				//_serialPortReward.DiscardInBuffer();
				//_serialPortReward.DiscardOutBuffer();
			}

			delta_T = Time.deltaTime;
			delta_z = rotaryTicks * speed;
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
			if (delta_z > 5.0f)
            {
				delta_z = 5.0f;
				Debug.Log("Tripped high delta z flag");
            }
			if (delta_z < -5.0f)
			{
				delta_z = -5.0f;
				Debug.Log("Tripped low delta z flag");
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
					transform.position = new Vector3(lastPosition[0], lastPosition[1], current_z + delta_z);
					if (transform.position.z < startOffsetStart)
                    {
						transform.position = new Vector3(lastPosition[0], lastPosition[1], startOffsetStart);
					}
				}
				lastPosition = transform.position;
            }

			//var sw_pos = new StreamWriter(positionFile, true);

			if (saveData == true)
			{ 
				try
				{
					int cueOrderPrint = -1;
					if (numTraversals >= numGainManipTrials)
					{
						if (numTraversals < numTrialsTotal)
						{
							cueOrderPrint = cueOrderList[numTraversals - numGainManipTrials];
						}
					}
					sw_pos.Write(transform.position.z + "\t" + Time.realtimeSinceStartup + "\t" + syncPinState + "\t" +
									pulseMarker + "\t" + startStopPinState + "\t" + delta_z + "\t" +
									lickPinValue + "\t" + rewardFlag + "\t" + numTraversals + "\t" + 
									cueOrderPrint + "\t" + delta_T + "\t" + missedArdFrame + "\n");
				}
				catch
                {
					Debug.Log("failed to write save file frame " + Time.frameCount);
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
		int currentCueOrder = cueOrderList[numTraversals - numGainManipTrials];

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
		if ((numTraversals - numGainManipTrials) >= 0)
		{
			int relObjPos = 0;
			int currentCueOrder = cueOrderList[numTraversals - numGainManipTrials];
			int[] cueOrderNow = new int[5];
			//Debug.Log("Made it here...");
			//Debug.Log("rop = " + relObjPos + ", currentCueOrder = " + currentCueOrder);

			//Debug.Log("SessionDay = " + SessionDay);
			//Debug.Log(dayCueOrders.Length);
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
						cueOrderNow[i] = dayCueOrders[SessionDay-1, currentCueOrder - 2, i];
						//Debug.Log("cur order " + i);
					}
					break;
			}
			for (int i = 0; i < 5; i++)
			{
				//Debug.Log("Cue " + i + " slot " + cueOrderNow[i]);
			}

			for (int i = 0; i < 5; i++)
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
				//Debug.Log("1");
				float relZpos = objectPositionsRel[relObjPos - 1];
				//Debug.Log("2");
				float targetZpos = (float)trackLen * relZpos;

				//Debug.Log("trackLen " + (float)trackLen);
				//Debug.Log("Moving object " + i + " to relative pos " + relZpos + " realDist " + targetZpos);
				//Debug.Log("3");
				GameObject objHere = ObjectCues[i];
				//Debug.Log("4");
				Vector3 pHere = objHere.transform.localPosition;
				//Debug.Log("5");
				pHere[2] = targetZpos;
				//Debug.Log("6");
				objHere.transform.localPosition = pHere;
				//Debug.Log("Obj " + i + " realPos is " + objHere.transform.localPosition[2]);

				//Debug.Log("bt " + blockType[numTraversals] + ", obj " + i + ", relObjPos " + relObjPos);
			}
			Debug.Log("Set the object positions for traversal " + (numTraversals - numGainManipTrials));
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
	/*
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
	*/
	private void Open()
	{
		_serialPort.Open();

		if (_serialPort.IsOpen)
		{
			Thread.Sleep(delay);
		}
	}
	/*
	private void OpenRwd()
	{
		_serialPortReward.Open();

		if (_serialPortReward.IsOpen)
		{
			Thread.Sleep(delay);
		}
	}
	*/
	private void Close()
	{
		if (_serialPort != null)
			_serialPort.Close();

		//if (_serialPortReward != null)
	//		_serialPortReward.Close();
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

		/*
		if (saveData == true)
		{
			File.Copy(trialTimesFile, serverTrialTimesFile);
			File.Copy(rewardFile, serverRewardFile);
			File.Copy(lickFile, serverLickFile);
			File.Copy(positionFile, serverPositionFile);
		}
		*/
	}

}
