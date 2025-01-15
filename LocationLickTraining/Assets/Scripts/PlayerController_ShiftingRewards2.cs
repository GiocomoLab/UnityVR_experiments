using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Threading;

public class PlayerController_ShiftingRewards2 : MonoBehaviour
{
	// arduino
	/*
	public Arduino arduino;
	private int rewardPin = 12; // for administering rewards
	private int stopPin = 9; // for stopping recording at end of session
	*/
	public string thisPort = "COM3";
	private SerialPort _serialPort;
	private int delay;

	public FadeRewardZone fadeRewardZone;

	private int missedArdFrame = 0;

	public enum ArduinoUse { Uniduino, SimulatedRunning };
	public ArduinoUse ArdUse;
	[HideInInspector]
	public bool simulatedRunning = false;
	public float simulatedRunningSpeed = 75.0f;

	public int trackUse = 1;

	public int lickPinValue;
	public int lickValThresh = 500;
	public int lickFlag = 0;

	private int floorLength;
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
	private float rewardPosition, rewardZoneStart, rewardZoneEnd;
	public float rewardZoneBuffer = 25f;
	//private bool lickForReward;
	private int rewardFlag = 0;
	private int automaticRewardFlag; // = 1;
	private int rewardCount;
	private float rewardZoneAlpha;

	private int totalLicks, anticipatoryLicks, rewardLicks, wrongLicks;
	private float anticipatoryLickRate, rewardLickRate, wrongLickRate;

	public GameObject rewardMarker;
	public GameObject visibleRewardZone;
	public bool useVisibleRewardZone = false;
	private Vector3 visRwZonePos;
	private float visRwZoneHeight;

	public int minTrialsTotal = 190;
	public int maxTrialsTotal = 200;
	private float rewardLocRel;// = 0.6f;
	private int rewardLocInt;
	private float trackOffset_x;

	private int numTraversalsDarkBefore = 0;
	private int numTraversalsDarkAfter = 0;

	// Timeout box
	public bool UseTimeout = true;
	public float TimeoutMinimum = 1.5f;
	public float TimeoutMaximum = 3.0f;
	private bool allowLapEnd = true;
	private float timeoutOver;
	private float timeoutDelay;
	private bool haveSetTimeoutEnd = false;
	[HideInInspector]
	public bool allowMovement = true;

	private bool dispRegLapsStart, dispDarkBeforeLapsStart, dispDarkAfterLapsStart;
	private bool doneDarkLapsBefore, doneRegularLaps;
	private bool doneShiftLaps = false;

	// saving data
	private string localDirectory, serverDirectory,localDirString, serverDirString;
	private SessionParams paramsScript;
	private string mouse, session;
	private bool saveData;
	private string trialTimesFile, serverTrialTimesFile;
	private string rewardFile, serverRewardFile;
	private string lickFile, serverLickFile;
	private string positionFile, serverPositionFile;
	private string startStopFile, serverStartStopFile;
	private string paramsTwoFile, serverParamsTwoFile;
	private string trialListFile, serverTrialListFile;
	private string positionHeaderFile, serverPositionHeaderFile;

	// teleport
	private float teleportPosition;
	private int trackLen;
	private bool justTeleported = false;

	private int cmdWrite = 0;
	private int pulseMarker = 1;
	private bool sentStartSignal = false;
	private int startStopPinState;
	private bool recordingStarted = false;

	private int target = 30;

	private StreamWriter sw_lick, sw_pos, sw_trial, sw_reward, sw_startstop, sw_par;

	private StreamWriter sw_pos_header;
	private bool wrotePosHeader = false;

	private bool endSessionE = false;
	private bool endSessionU = false;
	private bool dispEndSessE = false;
	private bool dispEndSessU = false;
	private bool ifSetSkipToEnd = false;
	private bool allowEndSession = false;
	private bool forceSessionEnd = false;

	public GameObject blackoutEnd;
	public float darkTrackOffset_x = -200.0f;
	public float darkTrackLength = 400.0f;
	public int numDarkLapsBefore = 30;
	public int numDarkLapsAfter = 30;
	public bool useRandomStartOffset = false;
	public float startOffsetMin = 0.0f;
	public float startOffsetMax = -100.0f;
	
	private int numRegularLaps;

	private bool sentBncPosLap = false;
	private float sendBncPosLapAt = 100.0f;
	private float lastZ = -1000.0f;
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

		initialPosition_z = 0.0f;
		teleportPosition = (float)trackLen;
		initialPosition = new Vector3(darkTrackOffset_x, 0.5f, initialPosition_z);
		transform.position = initialPosition;
		lastPosition = initialPosition;
		Debug.Log("initial position = " + initialPosition + ", transform.position = " + transform.position);

		if (numDarkLapsBefore == 0)
		{
			doneDarkLapsBefore = true;
		}

		// Set reward boundaries
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
			trialTimesFile = localDirString + "_trial_times.txt";
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

			sw_pos_header = new StreamWriter(positionHeaderFile, true);

			SaveSessionParameters();

			WriteRewardHeader();
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
		cameraDistReadOnly = cameraViewDist;

		if (Application.targetFrameRate != target)
			Application.targetFrameRate = target;

		// Set reward visibility, if using
		rwMarkerPos = new Vector3(trackOffset_x, -18f, rewardPosition);
		rewardMarker.transform.position = rwMarkerPos;
		visRwZoneHeight = visibleRewardZone.transform.position.y;
		visRwZonePos = new Vector3(trackOffset_x, visRwZoneHeight, rewardPosition);
		visibleRewardZone.transform.position = visRwZonePos;
		rewardZoneAlpha = fadeRewardZone.currentTransparency;

		if (recordingStarted == false)
		{
			allowMovement = false;
			if (Input.GetKeyDown(KeyCode.Return))
			{
				Debug.Log("starting Update");
				Debug.Log("numTraversals = " + numTraversals + " / numTrialsTotal = " + minTrialsTotal);
				Debug.Log("Trial " + (numTraversals + 1) + ", reward position = " + rewardPosition + " automaticRewardFlag = " + automaticRewardFlag);
				recordingStarted = true;
			}

			cmdWrite = 1;
		}

		CallArduinoUpdateMovementLocal();

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
			}
			
			cmdWrite = 1;

			if (Input.GetKeyDown(KeyCode.Space))
			{
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

			if (transform.position.z < (lastZ - 100.0f))
			{
				sentBncPosLap = false;
			}
			lastZ = transform.position.z;
			if (cmdWrite == 1)
			{
				if (sentBncPosLap == false)
				{
					if (transform.position.z >= sendBncPosLapAt)
					{
						cmdWrite = 7;
						sentBncPosLap = true;
					}
				}
			}

			// Dark laps before
			if (numTraversalsDarkBefore < numDarkLapsBefore && doneDarkLapsBefore == false)
			{
				if (dispDarkBeforeLapsStart == false) { Debug.Log("Starting early dark laps"); dispDarkBeforeLapsStart = true; }
				allowMovement = true;

				lastPosition.x = darkTrackOffset_x;

				if (lastPosition.z >= darkTrackLength)
				{
					lastPosition.z = 0.0f;
					numTraversalsDarkBefore++;
					if (numTraversalsDarkBefore >= numDarkLapsBefore) 
					{ 
						doneDarkLapsBefore = true;
						float thisStartOffset = 0.0f;
						if (useRandomStartOffset == true)
						{
							if (Mathf.Abs(startOffsetMin - startOffsetMax) > 0)
							{
								thisStartOffset = UnityEngine.Random.Range(startOffsetMin, Mathf.Abs(startOffsetMax));
								lastPosition.z = -1.0f*thisStartOffset;
							}
						}
					}
					Debug.Log("Done pre dark lap " + numTraversalsDarkBefore + " / " + numDarkLapsBefore);
				}

				transform.position = lastPosition;
			}

			// Normal lap running
			if (doneDarkLapsBefore == true && doneRegularLaps == false) //(numTraversalsDarkBefore >= numDarkLapsBefore)
			{
				if (dispRegLapsStart == false) { Debug.Log("Starting regular laps"); dispRegLapsStart = true; }
				allowMovement = true;

				lastPosition.x = trackOffset_x;

				// Track in-reward/anticipatory licking
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
						Debug.Log("Auto reward delivery " + " Trial= " + (numTraversals + 1) + ", numTraversalsThisLocation= " + numTraversalsThisLocation + ", automaticReward= " + automaticRewardFlag + ", num missed rewards= " + numMissedRewards + ", rewardsLickedThisLocation= " + rewardsLickedThisLocation);
						if (saveData)
						{
							WriteRewardFile();
						}

					}
				}

				// missed reward
				if (automaticRewardFlag == 0 & transform.position.z > rewardZoneEnd & rewardFlag == 0)
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
				}

				// teleport
				if (transform.position.z > teleportPosition) // | (transform.position.z < timeoutLimit)
				{
					allowLapEnd = true;

					lastPosition.z = teleportPosition + 0.1f;
					transform.position = lastPosition;

					if (endSessionU == true)
					{
						if (ifSetSkipToEnd == false)
						{
							Debug.Log("Recieved keypress sequence E - U, skipping to end of session");
							//numTraversals = maxTrialsTotal - 1;
							ifSetSkipToEnd = true;
							forceSessionEnd = true;
						}
					}

					if (UseTimeout == true)
					{
						float timeNow = Time.realtimeSinceStartup;
						
						if (haveSetTimeoutEnd == false)
						{

							float timeoutDelay = UnityEngine.Random.Range(TimeoutMinimum, TimeoutMaximum);
							timeoutOver = Time.realtimeSinceStartup + timeoutDelay;
							if (numTraversals + 1 == numRegularLaps) { timeoutOver = timeNow + 0.01f; } // Don't run a real delay, but have a frame in blackout on last lap, it'll get confusing with dark running
							haveSetTimeoutEnd = true;
							Debug.Log("Time out for " + timeoutDelay + "s" + "; This was at " + System.DateTime.Now);
						}

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
						// update number of trials
						numTraversals++;
						numTraversalsThisLocation++;
						rewardFlag = 0;
						Debug.Log("Ending lap: numTraversals = " + numTraversals + " / maxTrialsTotal = " + maxTrialsTotal);
						//Debug.Log("Reward summary: requested " + requestedRewards + ", automatic " + freeRewards + ", out of nlaps " + numTraversals);

						//Evaluate end of shift laps
						doneShiftLaps = false;
						if (numTraversals >= maxTrialsTotal)
                        {
							doneShiftLaps = true;
                        }
						if (numTraversals >= minTrialsTotal)
                        {
							if (numTraversalsThisLocation >= minTrialsPerLocation)
							{
								if (rewardsLickedThisLocation >= rewardsGetForNewLocation)
                                {
									doneShiftLaps = true;
                                }
                            }
                        }

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


						// At the end of the last lap, this causes it to tick up to num trials total, which with 0-intexing is one more than the listed number of trials
						Debug.Log("Ending lap: numTraversals = " + numTraversals + " / numTrialsTotal = " + minTrialsTotal + ", numTraversalsThisLocation = " + numTraversalsThisLocation);
						//teleportFlag = 1
						anticipatoryLickRate = (float)anticipatoryLicks / (float)totalLicks;
						rewardLickRate = (float)rewardLicks / (float)totalLicks;
						wrongLickRate = (float)wrongLicks / (float)totalLicks;
						//Debug.Log("Lick rates: correct = " + rewardLickRate + ", anticipatory = " + anticipatoryLickRate + ", wrong = " + wrongLickRate);

						if (doneShiftLaps == false) // Don't teleport on last sample
						{
							float thisStartOffset = 0.0f;
							if (useRandomStartOffset == true)
							{
								if (Mathf.Abs(startOffsetMin - startOffsetMax) > 0)
								{
									thisStartOffset = UnityEngine.Random.Range(startOffsetMin, Mathf.Abs(startOffsetMax));
								}
							}
							transform.position = new Vector3(trackOffset_x, 0.5f, 0.0f - thisStartOffset);
							lastPosition = transform.position;
							justTeleported = true;
						}

						if (doneShiftLaps == true)
						{
							Debug.Log("This was the last lap, numTraversals = " + numTraversals);
							doneRegularLaps = true;
						}

						if (forceSessionEnd == true)
                        {
							doneRegularLaps = true;
							doneDarkLapsBefore = true;
							//allowEndSession = true;
                        }

						if (saveData)
						{
							sw_trial.Write(Time.realtimeSinceStartup + "\t" + automaticRewardFlag + "\t" + numTraversals + "\n"); //"\t" + blockType[numTraversals] + 
						}

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
									if (Mathf.Abs(rewardPosition - oldRewardPosition) > minNewRewardDistance)
									{
										if (Mathf.Abs(rewardPosition - secondOldRewardPosition) > minNewRewardDistance)
										{
											haveRel = true;
											//Debug.Log(rewardLocRel + ", " + minRewardLocRel + ", " + maxRewardLocRel + ", " + rewardPosition + ", " + oldRewardPosition + ", " + secondOldRewardPosition + "\t" + minNewRewardDistance); //reward zone debug
											secondOldRewardPosition = oldRewardPosition;
										}
									}
								}
							}
							
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
								fadeRewardZone.numTraversals = numTraversals;
								fadeRewardZone.ResetTransparency();
							}
							catch
							{
								Debug.Log("Failed to reset visible reward zone transparency");
							}
						}

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
					}
				}

				if (doneRegularLaps == true)
				{
					lastPosition.x = darkTrackOffset_x;
					lastPosition.z = 0.0f;
					transform.position = lastPosition;
				}
				if ((transform.position.z > trackLen) && (transform.position.z < blackoutBoxPosition.z))
				{
					Debug.Log("beyond track end but not in box, numTraversals " + numTraversals);
				}
				/*
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
					*/
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
						Debug.Log("Done post dark lap " + numTraversalsDarkAfter + " / " + numDarkLapsAfter);
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
					}
				}
				transform.position = lastPosition;
			}

			SaveUpdateDataLocal();

			// For dark running
			Vector3 blackoutPos = blackoutEnd.transform.position;
			blackoutPos.z = lastPosition.z + 15.0f;
			blackoutEnd.transform.position = blackoutPos;

			if (allowEndSession)
			{
				Debug.Log("Stopping session at " + Time.realtimeSinceStartup + "\n");
				sw_startstop.Write("StopSignalPlanned" + "\t" + Time.realtimeSinceStartup + "\n");
				UnityEditor.EditorApplication.isPlaying = false;
			}

			pulseMarker = pulseMarker * -1;
			//pulseMarker *= -1;
		} //recordingStarted true
	} // update

	void SaveUpdateDataLocal()
    {
		if (saveData == true)
		{
			try
			{

				int blackoutLap = 0;
				if ((transform.position.x < (darkTrackOffset_x + 1.0f)) && (transform.position.x > (darkTrackOffset_x - 1.0f)))
				{
					//blackoutLap = 1;
				}
				int numTraversalsWrite = numTraversals;
				if (numDarkLapsBefore > 0) 
				{ 
					if (doneDarkLapsBefore == false) 
					{ 
						numTraversalsWrite = numTraversalsDarkBefore - numDarkLapsBefore;
						blackoutLap = 1;
					} 
				}
				//if (doneDarkLapsBefore == false) { numTraversalsWrite = numTraversalsDarkBefore - numDarkLapsBefore; }
				if (doneRegularLaps == true) { numTraversalsWrite = numTraversalsDarkAfter + (numTraversals); blackoutLap = 1; };

				sw_pos.Write(transform.position.z + "\t" + Time.realtimeSinceStartup + "\t" + syncPinState + "\t" +
								pulseMarker + "\t" + startStopPinState + "\t" + delta_z + "\t" +
								lickPinValue + "\t" + rewardFlag + "\t" + numTraversalsWrite + "\t" +
								delta_T + "\t" + missedArdFrame + "\t" + blackoutLap + "\n");

				if (wrotePosHeader == false)
				{
					sw_pos_header.Write("transform.position.z" + "\t" + "Time.realtimeSinceStartup" + "\t" + "syncPinState" + "\t" +
								"pulseMarker" + "\t" + "startStopPinState" + "\t" + "delta_z" + "\t" +
								"lickPinValue" + "\t" + "rewardFlag" + "\t" + "numTraversals" + "\t" +
								"delta_T" + "\t" + "missedArdFrame" + "\t" + "blackoutLap" + "\n");
					wrotePosHeader = true;
				}
			}
			catch
			{
				Debug.Log("failed to write save file frame " + Time.frameCount);
			}
		}
	}

	void CallArduinoUpdateMovementLocal()
	{

		missedArdFrame = 0;
		if (ArdUse == ArduinoUse.Uniduino)
		{
			_serialPort.Write(cmdWrite.ToString() + ',');
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

				if (lickPinValue > lickValThresh)
				{
					lickFlag = 0;
				}
				if (lickPinValue <= lickValThresh)
				{
					numLicks += 1;
					lickFlag = 1;

					if (saveData)
					{
						sw_lick.Write(transform.position.z + "\t" + Time.realtimeSinceStartup + "\n");
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
				Debug.Log("cmd " + cmdWrite + " reply " + lick_raw + " frame " + Time.frameCount);
				missedArdFrame = 1;
			}

			_serialPort.DiscardInBuffer();
			_serialPort.DiscardOutBuffer();
		}

		delta_T = Time.deltaTime;
		delta_z = rotaryTicks * speed;
		if (simulatedRunning == false)
		{
			float approxSpeed = delta_z / delta_T;
			if (Mathf.Abs(approxSpeed) > 100.0f)
			{
				delta_z = 100.0f * delta_T;
				if (approxSpeed < 0.0f) { delta_z = delta_z * -1.0f; }
			}
		}

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
				//if (new_z < -5.0f)
				//{
				//	new_z = 0.0f;
				//}
				transform.position = new Vector3(lastPosition[0], lastPosition[1], new_z);
			}
			lastPosition = transform.position;
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
		sw_par.Write("darkTrackOffset_x" + "\t" + darkTrackOffset_x + "\n");
		sw_par.Write("darkTrackLength" + "\t" + darkTrackLength + "\n");
		sw_par.Write("numDarkLapsBefore" + "\t" + numDarkLapsBefore + "\n");
		sw_par.Write("numDarkLapsAfter" + "\t" + numDarkLapsAfter + "\n");
		sw_par.Write("useRandomStartOffset" + "\t" + useRandomStartOffset + "\n");
		sw_par.Write("startOffsetMin" + "\t" + startOffsetMin + "\n");
		sw_par.Write("startOffsetMax" + "\t" + startOffsetMax + "\n");
		sw_par.Write("minTrialsPerLocation" + "\t" + minTrialsPerLocation + "\n");
		sw_par.Write("maxTrialsPerLocation" + "\t" + maxTrialsPerLocation + "\n");
		sw_par.Write("rewardsGetForNewLocation" + "\t" + rewardsGetForNewLocation + "\n");
		sw_par.Write("freeRewardsEachLocation" + "\t" + freeRewardsEachLocation + "\n");
		sw_par.Write("rewardMissesBeforeReminder" + "\t" + rewardMissesBeforeReminder + "\n");
		sw_par.Write("requireLickForReward" + "\t" + requireLickForReward + "\n");
		sw_par.Write("minRewardLocRel" + "\t" + minRewardLocRel + "\n");
		sw_par.Write("maxRewardLocRel" + "\t" + maxRewardLocRel + "\n");


	sw_par.Write("minNewRewardDistance", minNewRewardDistance);
}
	void WriteRewardHeader()
	{
		sw_reward.Write("realtimeSinceStartup" + "\t" + "rewardCount" + "\t" + "rewardPosition" + "\t" + "numTraversals" + "\t" + "rewardZoneAlpha" + "\t" + "automaticRewardFlag" + "\t" + "gotAutomaticRewad" + "\t" + "missedReward" + "\n");
	}

	void WriteRewardFile()
	{

		sw_reward.Write(Time.realtimeSinceStartup + "\t" + rewardCount + "\t" + rewardPosition + "\t" + numTraversals + "\t" + rewardZoneAlpha + "\t" + automaticRewardFlag + "\t" + gotAutomaticReward + "\t" + missedReward + "\n");
	}

	private void connect(string serialPortName, Int32 baudRate, bool autoStart, int delay)
	{
		_serialPort = new SerialPort(serialPortName, baudRate);

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
				File.Copy(positionHeaderFile, serverPositionHeaderFile);
				Debug.Log("Copied the positionHeader file");
			}
			catch
			{
				Debug.Log("something did not copy to the server");
			}
		}

	}
}
