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

public class PlayerController_ShiftingRewards : MonoBehaviour
{
	// arduino
	/*
	public Arduino arduino;
	private int rewardPin = 12; // for administering rewards
	private int stopPin = 9; // for stopping recording at end of session
	*/
	public string thisPort = "COM6";
	private SerialPort _serialPort;
	private int delay;

	public FadeRewardZone fadeRewardZone;
	//public string rewardPort = "COM5";
	//private SerialPort _serialPortReward;

	private int missedArdFrame = 0;

	public enum ArduinoUse { Uniduino, SimulatedRunning };
	public ArduinoUse ArdUse;
	[HideInInspector]
	public bool simulatedRunning = false;
	public float simulatedRunningSpeed = 75.0f;

	public int trackUse = 1;

	public int lickPinValue;
	public int lickValThresh = 250;
	public int lickFlag = 0;

	private int floorLength;
	//private Component frontCamera = GetComponentInChildren<panoCamera.subCam1>;
	private int cameraViewDist = 165;
	public int cameraDistReadOnly;

	// movement / trial tracking
	private float initialPosition_z = 0.0f; // The Start of the 
	private Vector3 initialPosition;
	private Vector3 lastPosition;
	//private Vector3 blackoutBoxPosition = new Vector3(0f, -24.0f, 526.0f);
	private Vector3 blackoutBoxPosition;
	private Vector3 blackoutBoxOffsets = new Vector3(0f, -24.0f, 26.0f);
	private Vector3 rwMarkerPos;
	public int numTraversals = 0;

	private string lick_raw;

	private int numLicks = 0;

	private int rotaryTicks = 0;
	private float speed = 0.0447f;
	private int syncPinState;
	//private int arduinoRespondedFlag = 0;
	private float delta_z;
	private float current_z;
	private float delta_T;

	// Reward stuff for main track
	public int minTrialsPerLocation = 15;
	public int maxTrialsPerLocation = 40;
	public int rewardsGetForNewLocation = 10;
	public int freeRewardsEachLocation = 5; // How many rewards delivered automatically at the start of the session
	public int rewardMissesBeforeReminder = 3; // How many trials mouse can miss before automatic reminder reward
	public bool requireLickForReward = true;
	private int numMissedRewards = 0;
	private int totalNonAutoRewards = 0;
	private int totalMissedRewards = 0;
	private int totalRequestedRewards = 0;
	private int rewardsLickedThisLocation = 0;
	private int numTraversalsThisLocation = 0;
	private float minRewardLocRel = 0.1f;
	private float maxRewardLocRel = 0.9f;
	private int missedReward, gotAutomaticReward;
	private float secondOldRewardPosition, minNewRewardDistance;
	//private float minDisttoNewRewadRel; // use size of reward zone buffer

	// for each trial's reward
	private float rewardPosition;
	private float rewardZoneStart;
	private float rewardZoneEnd;
	public float rewardZoneBuffer = 25f;
	private bool lickForReward;
	private int rewardFlag = 0;
	private int automaticRewardFlag; // = 1;
	private int rewardCount;
	private float rewardZoneAlpha;

	private int totalLicks;
	private int anticipatoryLicks;
	private int rewardLicks;
	private int wrongLicks;
	private float anticipatoryLickRate, rewardLickRate, wrongLickRate;

	public GameObject rewardMarker;
	public GameObject visibleRewardZone;
	public bool useVisibleRewardZone = false;
	private Vector3 visRwZonePos;
	private float visRwZoneHeight;

	public int minTrialsTotal = 200;
	public int maxTrialsTotal = 400;
	private float rewardLocRel;// = 0.6f;
	private int rewardLocInt;
	private float trackOffset_x;

	// Timeout box
	public bool UseTimeout = true;
	public float TimeoutMinimum = 2.5f;
	public float TimeoutMaximum = 2.5f;
	public float BlackRunningSeconds = 120.0f;
	private bool allowLapEnd = true;
	private float timeoutOver;
	private float timeoutDelay;
	private bool haveSetTimeoutEnd = false;
	[HideInInspector]
	public bool allowMovement = true;

	// saving data
	private string localDirectory;
	private string serverDirectory;
	private string localDirString;
	private string serverDirString;
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
	private string positionFile, serverPositionFile;
	//private string serverPositionFile;
	private string startStopFile;
	private string serverStartStopFile;
	private string paramsTwoFile;
	private string serverParamsTwoFile;
	private string trialListFile;
	private string serverTrialListFile;

	private string positionHeaderFile;
	private string serverPositionHeaderFile;

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

	private StreamWriter sw_pos_header;
	private bool wrotePosHeader = false;

	private bool endSessionE = false;
	private bool endSessionU = false;
	private bool dispEndSessE = false;
	private bool dispEndSessU = false;
	private bool ifSetSkipToEnd = false;

	private void Awake()
	{
		Debug.Log("wake");
		if (!((trackUse == 1) || (trackUse == 2) || (trackUse == 3) || (trackUse == 4)))
		{
			Debug.Log("Bad session day integer, quitting");
			UnityEditor.EditorApplication.isPlaying = false;
		}

		// These should go shortest -x to longest +x
		if (trackUse == 1)
		{
			// Obvious pattern
			trackOffset_x = -400.0f;
			//rewardLocRel = 0.95f;
			rewardLocInt = 225;
			trackLen = 250;
			rewardLocRel = (float)rewardLocInt / (float)trackLen;
		}
		else if (trackUse == 2)
		{
			trackOffset_x = -200.0f;
			rewardLocRel = 0.35f; //140
			trackLen = 400;
		}
		else if (trackUse == 3)
		{
			trackOffset_x = 0.0f;
			rewardLocRel = 0.75f; // 350
			trackLen = 500;
		}
		else if (trackUse == 4)
		{
			trackOffset_x = 200.0f;
			rewardLocRel = 0.92f; // 404
			trackLen = 450;
		}

		// initialize arduino
		if (ArdUse == ArduinoUse.Uniduino)
		{
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
			_serialPort.DiscardInBuffer();
			_serialPort.DiscardOutBuffer();
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

		blackoutBoxPosition = new Vector3(trackOffset_x, 0.0f, (float)trackLen);
		blackoutBoxPosition = blackoutBoxPosition + blackoutBoxOffsets;
		Debug.Log("BlackoutBox position = " + blackoutBoxPosition);

		//initialPosition_z = paramsScript.trackStart;
		initialPosition_z = 0.0f;
		//teleportPosition = paramsScript.trackEnd;
		teleportPosition = (float)trackLen;
		//trackLen = (int)teleportPosition;
		initialPosition = new Vector3(trackOffset_x, 0.5f, initialPosition_z);
		transform.position = initialPosition;
		lastPosition = initialPosition;
		Debug.Log("initial position = " + initialPosition + ", transform.position = " + transform.position);

		// reward params
		//lickForReward = paramsScript.lickForReward;

		// Set reward boundaries
		//minNewRewardDistance = rewardZoneBuffer * 3;
		minNewRewardDistance = 50.0f;
		var randomRel = new System.Random();
		bool haveRel = false;
		while (haveRel == false)
		{
			rewardLocRel = (float)randomRel.NextDouble();
			if (rewardLocRel >= minRewardLocRel & rewardLocRel <= maxRewardLocRel)
			{
				haveRel = true;
			}
		}

		rewardPosition = trackLen * rewardLocRel;
		secondOldRewardPosition = rewardPosition;
		rewardZoneStart = rewardPosition - rewardZoneBuffer;
		rewardZoneEnd = rewardPosition + rewardZoneBuffer;
		if ((freeRewardsEachLocation > 0) || (requireLickForReward == false))
		{
			automaticRewardFlag = 1;
		}
		Debug.Log("trackLen = " + trackLen + ", rewardLocRel = " + rewardLocRel + ", rewardPosition = " + rewardPosition);

		rwMarkerPos = new Vector3(trackOffset_x, -18f, rewardPosition);
		rewardMarker.transform.position = rwMarkerPos;
		visRwZoneHeight = -25f;
		if (useVisibleRewardZone == true)
		{
			visRwZoneHeight = 0.5f;
		}
		visRwZonePos = new Vector3(trackOffset_x, visRwZoneHeight, rewardPosition);
		visibleRewardZone.transform.position = visRwZonePos;

		// for saving data	
		mouse = paramsScript.mouse;
		session = paramsScript.session;
		saveData = paramsScript.saveData;

		if (saveData == true)
		{
			localDirectory = paramsScript.localDirectory;
			serverDirectory = paramsScript.serverDirectory;
			localDirString = paramsScript.localDirectory + "\\" + mouse + "\\" + session;
			serverDirString = paramsScript.serverDirectory + "\\" + mouse + "\\" + session;
			//trialTimesFile = localDirectory + "\\" + mouse + "\\" + session + "_trial_times.txt";
			trialTimesFile = localDirString + "_trial_times.txt";
			//serverTrialTimesFile = serverDirectory + "\\" + mouse + "\\" + session + "_trial_times.txt";
			serverTrialTimesFile = serverDirString + "_trial_times.txt";
			rewardFile = localDirString + "_reward.txt";
			serverRewardFile = serverDirectory + "\\" + mouse + "\\" + session + "_reward.txt";
			positionFile = localDirString + "_position.txt";
			serverPositionFile = serverDirString + "_position.txt";
			lickFile = localDirString + "_licks.txt";
			serverLickFile = serverDirString + "_licks.txt";
			startStopFile = localDirString + "_startStop.txt";
			serverStartStopFile = serverDirString + "_startStop.txt";
			paramsTwoFile = localDirString + "_params2.txt";
			serverParamsTwoFile = serverDirString + "_params2.txt";
			trialListFile = localDirString + "_trialList.txt";
			serverTrialListFile = serverDirString + "_trialList.txt";

			positionHeaderFile = localDirString + "_positionHeader.txt";
			serverPositionHeaderFile = serverDirString + "_positionHeader.txt";

			if (File.Exists(positionFile))
			{
				Debug.Log("This file already exists!");
				int numLines = File.ReadAllLines(positionFile).Length;
				Debug.Log("It has " + numLines + " lines in it");

				if (numLines > 1)
				{
					UnityEditor.EditorApplication.isPlaying = false;
				}
			}

			sw_lick = new StreamWriter(lickFile, true);
			sw_pos = new StreamWriter(positionFile, true);
			sw_trial = new StreamWriter(trialTimesFile, true);
			sw_reward = new StreamWriter(rewardFile, true);
			sw_startstop = new StreamWriter(startStopFile, true);
			sw_par = new StreamWriter(paramsTwoFile, true);
			sw_trialList = new StreamWriter(trialListFile, true);

			sw_pos_header = new StreamWriter(positionHeaderFile, true);

			SaveSessionParameters();
		}
		else
		{
			//Debug.Log("Warning: this is NOT saving any data");
		}

		Debug.Log("Press return to start the session");

		if (Application.targetFrameRate != target)
			Application.targetFrameRate = target;
	}

	void Update()
	{
		cameraDistReadOnly = cameraViewDist;

		//missedArdFrame = 0;
		if (Application.targetFrameRate != target)
			Application.targetFrameRate = target;

		rwMarkerPos = new Vector3(trackOffset_x, -18f, rewardPosition);
		rewardMarker.transform.position = rwMarkerPos;
		visRwZoneHeight = visibleRewardZone.transform.position.y;
		visRwZonePos = new Vector3(trackOffset_x, visRwZoneHeight, rewardPosition);
		visibleRewardZone.transform.position = visRwZonePos;
		rewardZoneAlpha = fadeRewardZone.currentTransparency;

		if (recordingStarted == false)
		{
			if (Input.GetKeyDown(KeyCode.Return))
			{
				Debug.Log("starting Update");
				Debug.Log("numTraversals = " + numTraversals + " / numTrialsTotal = " + minTrialsTotal);
				Debug.Log("Trial " + (numTraversals + 1) + ", reward position = " + rewardPosition + " automaticRewardFlag = " + automaticRewardFlag);
				recordingStarted = true;
			}

			cmdWrite = 1;
			missedArdFrame = 0;
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
					if (lickPinValue > lickValThresh)
					{
						lickFlag = 0;
					}
					//if (lickPinValue < 500 & lickFlag == 0)
					if (lickPinValue <= lickValThresh)
					{
						numLicks += 1;
						lickFlag = 1;
					}
				}
				catch (TimeoutException)
				{
					Debug.Log("lickport/encoder timeout on frame " + Time.frameCount);
					missedArdFrame = 1;
				}
				catch
				{
					Debug.Log("cmd " + cmdWrite + " reply " + lick_raw + " frame " + Time.frameCount);
					missedArdFrame = 1;
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

				if (ArdUse == ArduinoUse.Uniduino)
				{
					//_serialPortReward.DiscardInBuffer();
					//_serialPortReward.DiscardOutBuffer();
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

			// Track in reward/anticipatory licking
			if ((lickFlag == 1) & (rewardFlag == 0))
			{
				totalLicks += 1;
				if ((transform.position.z > (rewardZoneStart - rewardZoneBuffer)) & (transform.position.z <= rewardZoneStart))
				{
					// Anticipatory licking
					anticipatoryLicks += 1;
				}
				else if ((transform.position.z > rewardZoneStart) & (transform.position.z <= rewardZoneEnd))
				{
					// In reward zone
					rewardLicks += 1;
				}
				else
				{
					// Wrong zone
					wrongLicks += 1;
				}

			}

			// Reward
			missedReward = 0; gotAutomaticReward = 0;
			if (transform.position.z > rewardZoneStart & transform.position.z < rewardZoneEnd & rewardFlag == 0)
			{
				if (lickFlag == 1)
				{
					rewardFlag = 1;
					cmdWrite = 2;

					//rewardNumThisLocation += 1;
					rewardsLickedThisLocation += 1;

					numMissedRewards = 0;
					rewardCount += 1;
					totalRequestedRewards = totalRequestedRewards + 1;
					totalNonAutoRewards = totalNonAutoRewards + 1;

					// update debug log and save info
					Debug.Log("Reward requested " + " Trial= " + (numTraversals + 1) + "numTraversalsThisLocation= " + numTraversalsThisLocation + ", automaticReward= " + automaticRewardFlag + ", num missed rewards= " + numMissedRewards + ", rewardsLickedThisLocation= " + rewardsLickedThisLocation);
					Debug.Log("Requested " + totalRequestedRewards + " out of " + totalNonAutoRewards);

					if (saveData)
					{
						WriteRewardFile();
					}
				}

				// automatic reward
				if (automaticRewardFlag == 1 & transform.position.z > rewardPosition)
				{
					rewardFlag = 1;
					cmdWrite = 2;
					numMissedRewards = 0;

					rewardCount += 1;

					// update debug log
					gotAutomaticReward = 1;
					Debug.Log("Auto reward delivery " + " Trial= " + (numTraversals + 1) + "numTraversalsThisLocation= " + numTraversalsThisLocation + ", automaticReward= " + automaticRewardFlag + ", num missed rewards= " + numMissedRewards + ", rewardsLickedThisLocation= " + rewardsLickedThisLocation);
					if (saveData)
					{
						WriteRewardFile();
					}
				}
			}

			// missed reward
			if (automaticRewardFlag == 0 & transform.position.z > rewardZoneEnd & rewardFlag == 0) // AUto flag should be unnecessary, just a backup?
			{
				rewardFlag = 1;
				numMissedRewards = numMissedRewards + 1;
				totalMissedRewards = totalMissedRewards + 1;
				totalNonAutoRewards = totalNonAutoRewards + 1;

				// update debug log 
				Debug.Log("Missed Reward, " + " Trial= " + (numTraversals + 1) + "numTraversalsThisLocation= " + numTraversalsThisLocation + ", automaticReward= " + automaticRewardFlag + ", num missed rewards= " + numMissedRewards + ", rewardsLickedThisLocation= " + rewardsLickedThisLocation);
				Debug.Log("Requested " + totalRequestedRewards + " out of " + totalNonAutoRewards);
				missedReward = 1;
				if (saveData)
				{
					WriteRewardFile();
				}

				/*
				if (numMissedRewards >= rewardMissesBeforeReminder)
				{
					automaticRewardFlag = 1;
					numMissedRewards = 0;
					Debug.Log("Next trial automatic reward");
				}
				*/
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
						numTraversals = maxTrialsTotal - 1;
						ifSetSkipToEnd = true;
					}
				}

				if (UseTimeout == true)
				{
					numTraversals += 1;
					numTraversalsThisLocation += 1; // Terrible hacky way to do this, please fix in future versions

					bool newRewardLocation = false;
					if (numTraversalsThisLocation >= minTrialsPerLocation)
					{
						if (numTraversalsThisLocation >= maxTrialsPerLocation)
						{
							newRewardLocation = true;
						}

						if (rewardsLickedThisLocation >= rewardsGetForNewLocation)
						{
							newRewardLocation = true;
						}
						//Debug.Log("Eval new reward position: " + "numTraversalsThisLocation=" + numTraversalsThisLocation + ", rewardsLickedThisLocation=" + rewardsLickedThisLocation + ", set new position = " + newRewardLocation);
					}

					if (haveSetTimeoutEnd == false)
					{
						//if ((numTraversals >= minTrialsTotal & rewardsLickedThisLocation >= rewardsGetForNewLocation) || (numTraversals >= maxTrialsTotal)) // end session conditions
						if (((numTraversals >= minTrialsTotal) & (rewardsLickedThisLocation >= rewardsGetForNewLocation)) || ((numTraversals >= minTrialsTotal) & (numTraversals >= maxTrialsTotal)) || ((numTraversals >= minTrialsTotal) & (newRewardLocation == true) & ((maxTrialsTotal - numTraversals) < minTrialsPerLocation)))
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
						Debug.Log("Time out for " + timeoutDelay + "s" + " at " + System.DateTime.Now);

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

					numTraversals -= 1;
					numTraversalsThisLocation -= 1; // Terrible hacky way to do this, please fix in future versions
				}

				if (allowLapEnd == true)
				{
					// update number of trials
					numTraversals += 1;
					numTraversalsThisLocation += 1;
					// At the end of the last lap, this causes it to tick up to num trials total, which with 0-intexing is one more than the listed number of trials
					Debug.Log("Ending lap: numTraversals = " + numTraversals + " / numTrialsTotal = " + minTrialsTotal + ", numTraversalsThisLocation = " + numTraversalsThisLocation);
					//teleportFlag = 1
					anticipatoryLickRate = (float)anticipatoryLicks / (float)totalLicks;
					rewardLickRate = (float)rewardLicks / (float)totalLicks;
					wrongLickRate = (float)wrongLicks / (float)totalLicks;
					Debug.Log("Lick rates: correct = " + rewardLickRate + ", anticipatory = " + anticipatoryLickRate + ", wrong = " + wrongLickRate);

					bool newRewardLocation = false;
					if (numTraversalsThisLocation >= minTrialsPerLocation)
					{
						if (numTraversalsThisLocation >= maxTrialsPerLocation)
						{
							newRewardLocation = true;
						}

						if (rewardsLickedThisLocation >= rewardsGetForNewLocation)
						{
							newRewardLocation = true;
						}
						Debug.Log("Eval new reward position: " + "numTraversalsThisLocation=" + numTraversalsThisLocation + ", rewardsLickedThisLocation=" + rewardsLickedThisLocation + ", set new position = " + newRewardLocation);
					}

					// write trial time to file
					if (numTraversals < minTrialsTotal)
					{
						if (saveData)
						{
							sw_trial.Write(Time.realtimeSinceStartup + "\t" + automaticRewardFlag + "\t" + numTraversals + "\n"); //"\t" + blockType[numTraversals] + 
						}
					}
					else // more than minimum number of trials
					{
						// end session conditions
						Debug.Log("last lap eval, " + "rewardsLickedThisLocation= " + rewardsLickedThisLocation + ", numTraversals= " + numTraversals + ", minTrialsTotal " + minTrialsTotal);
						//        got enough reards this block (redundant)			 OR		hit max possible trials		 OR		going to set a new reward location AND not enough trials left
						if ((rewardsLickedThisLocation >= rewardsGetForNewLocation) || (numTraversals >= maxTrialsTotal) || ((newRewardLocation == true) & ((maxTrialsTotal - numTraversals) < minTrialsPerLocation)))
						{
							Debug.Log("this was the last lap, numTraversals = " + numTraversals);
							triggerStopSession = true;
							allowMovement = false;
							Debug.Log("trigger stop session = " + triggerStopSession);
							numTraversals = numTraversals - 1; // For better marking position file at end of session
						}
						else
						{
							if (saveData)
							{
								sw_trial.Write(Time.realtimeSinceStartup + "\t" + automaticRewardFlag + "\t" + numTraversals + "\n"); //"\t" + blockType[numTraversals] + 
							}
						}
					}

					//if (numTraversals < maxTrialsTotal) // Don't teleport on last sample
					//{
					if (triggerStopSession == false)
					{
						transform.position = initialPosition;
						lastPosition = transform.position;
						justTeleported = true;
					}
					//}

					rewardFlag = 0;

					if (newRewardLocation == true)
					{
						float oldRewardPosition = rewardPosition;
						var randomRel = new System.Random();
						bool haveRel = false;
						while (haveRel == false)
						{
							rewardLocRel = (float)randomRel.NextDouble();
							if (rewardLocRel >= minRewardLocRel & rewardLocRel <= maxRewardLocRel)
							{
								rewardPosition = trackLen * rewardLocRel;
								//if (((rewardPosition - oldRewardPosition) > rewardZoneBuffer * 2.5) || ((rewardPosition - oldRewardPosition) < -rewardZoneBuffer * 2.5))
								if (Mathf.Abs(rewardPosition - oldRewardPosition) > minNewRewardDistance)
								{
									//if (((rewardPosition - secondOldRewardPosition) > rewardZoneBuffer * 2.5) || ((rewardPosition - secondOldRewardPosition) < -rewardZoneBuffer * 2.5))
									if (Mathf.Abs(rewardPosition - secondOldRewardPosition) > minNewRewardDistance)
									{
										haveRel = true;
										Debug.Log(rewardLocRel + ", " + minRewardLocRel + ", " + maxRewardLocRel + ", " + rewardPosition + ", " + oldRewardPosition + ", " + secondOldRewardPosition + "\t" + minNewRewardDistance); //reward zone debug
										secondOldRewardPosition = oldRewardPosition;
									}
								}
							}
						}
						//
						rewardZoneStart = rewardPosition - rewardZoneBuffer;
						rewardZoneEnd = rewardPosition + rewardZoneBuffer;
						if ((freeRewardsEachLocation > 0) || (requireLickForReward == false))
						{
							automaticRewardFlag = 1;
						}
						numMissedRewards = 0;
						rewardsLickedThisLocation = 0;
						numTraversalsThisLocation = 0;
						Debug.Log("New reward position" + ", rewardLocRel = " + rewardLocRel + ", rewardPosition = " + rewardPosition);

						try
						{
							fadeRewardZone.ResetTransparency();
						}
						catch
						{
							Debug.Log("Failed to reset visible reward zone transparency");
						}
					}

					//if (requireLickForReward == true)
					//{
					automaticRewardFlag = 0;
					if (numTraversalsThisLocation < freeRewardsEachLocation)
					{
						automaticRewardFlag = 1;
						Debug.Log("Next trial auto reward available for " + freeRewardsEachLocation + " free each location");
					}

					if (numMissedRewards >= rewardMissesBeforeReminder)
					{
						automaticRewardFlag = 1;
						numMissedRewards = 0;
						Debug.Log("Next trial automatic reward for " + rewardMissesBeforeReminder + " misses");
					}
					//}
				}
			}

			missedArdFrame = 0;
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
					if (lickPinValue > lickValThresh)
					{
						lickFlag = 0;
					}
					//if (lickPinValue < 500 & lickFlag == 0)
					if (lickPinValue < lickValThresh)
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
					//Debug.Log("lickport/encoder timeout on frame " + Time.frameCount);
					missedArdFrame = 1;
				}
				catch
				{
					Debug.Log("cmd " + cmdWrite + " reply " + lick_raw + " frame " + Time.frameCount);
					missedArdFrame = 1;
				}

				if (cmdWrite == 2)
				{
					try
					{
						//_serialPortReward.Write(cmdWrite.ToString() + ',');
					}
					catch
					{
						Debug.Log("failed to write reward command");
					}
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
					//transform.position = new Vector3(lastPosition[0], lastPosition[1], current_z + delta_z);
					transform.position = new Vector3(lastPosition[0], lastPosition[1], new_z);
				}
				lastPosition = transform.position;
			}

			if (saveData == true)
			{
				try
				{
					int cueOrderPrint = 0;
					sw_pos.Write(transform.position.z + "\t" + Time.realtimeSinceStartup + "\t" + syncPinState + "\t" +
									pulseMarker + "\t" + startStopPinState + "\t" + delta_z + "\t" +
									lickPinValue + "\t" + rewardFlag + "\t" + numTraversals + "\t" +
									cueOrderPrint + "\t" + delta_T + "\t" + missedArdFrame + "\n");

					if (wrotePosHeader == false)
					{
						sw_pos_header.Write("transform.position.z" + "\t" + "Time.realtimeSinceStartup" + "\t" + "syncPinState" + "\t" +
									"pulseMarker" + "\t" + "startStopPinState" + "\t" + "delta_z" + "\t" +
									"lickPinValue" + "\t" + "rewardFlag" + "\t" + "numTraversals" + "\t" +
									"cueOrderPrint" + "\t" + "delta_T" + "\t" + "missedArdFrame" + "\n");
						wrotePosHeader = true;
					}
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

		if (missedArdFrame == 1)
		{
			//Debug.Log('Missed arduino frame here');
		}
	}

	void SaveSessionParameters()
	{
		// Save some params
		sw_par.Write("trackUse" + "\t" + trackUse + "\n");
		sw_par.Write("trackLen" + "\t" + trackLen + "\n");
		sw_par.Write("lickValThresh" + "\t" + lickValThresh + "\n");
		//sw_par.Write("rewardLocRel" + "\t" + rewardLocRel + "\n");
		//sw_par.Write("rewardLocInt" + "\t" + rewardLocInt + "\n");
		sw_par.Write("rewardZoneBuffer" + "\t" + rewardZoneBuffer + "\n");
		sw_par.Write("rewardMissesBeforeReminder" + "\t" + rewardMissesBeforeReminder + "\n");
		sw_par.Write("requireLickForReward" + "\t" + requireLickForReward + "\n");
		sw_par.Write("blackoutBoxPosition" + "\t" + blackoutBoxPosition + "\n");
		sw_par.Write("UseTimeout" + "\t" + UseTimeout + "\n");
		sw_par.Write("TimeoutMinimum" + "\t" + TimeoutMinimum + "\n");
		sw_par.Write("TimeoutMaximum" + "\t" + TimeoutMaximum + "\n");
		sw_par.Write("BlackRunningSeconds" + "\t" + BlackRunningSeconds + "\n");
	}

	void WriteRewardFile()
	{
		sw_reward.Write(Time.realtimeSinceStartup + "\t" + rewardCount + "\t" + rewardPosition + "\t" + numTraversals + "\t" + rewardZoneAlpha + "\t" + gotAutomaticReward + "\t" + missedReward + "\n");
	}

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

	private void Open()
	{
		_serialPort.Open();

		if (_serialPort.IsOpen)
		{
			Thread.Sleep(delay);
		}
	}

	private void Close()
	{
		if (_serialPort != null)
			_serialPort.Close();
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
			try
			{
				_serialPort.Write(cmdWrite.ToString() + ',');
			}
            catch
            {

            }
		}

		Debug.Log("saving data");
		if (saveData == true)
		{
			sw_startstop.Write("StopSignalSent" + "\t" + Time.realtimeSinceStartup + "\n");

			sw_startstop.Close();
			sw_trial.Close();
			sw_pos.Close();
			sw_reward.Close();
			sw_lick.Close();
			sw_par.Close();

			sw_pos_header.Close();


			Debug.Log("copying data to server");
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
				File.Copy(paramsTwoFile, serverParamsTwoFile);
				Debug.Log("Copied the paramsTwo file");
				File.Copy(trialListFile, serverTrialListFile);
				Debug.Log("Copied the trialList file");
			}
			catch
			{
				Debug.Log("something did not copy to the server");
			}
		}

	}
}
