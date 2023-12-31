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
using System.Net;
using System.Net.Mail;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

public class PlayerController2 : MonoBehaviour 
{

	public string thisPort = "COM6";
	private SerialPort _serialPort;
	private int delay;

	public string rewardPort = "COM5";
	private SerialPort _serialPortReward;

	public GameObject rewardZone;

	private string lick_raw;
	public int lickPinValue;
	private int numLicks = 0;
	public int lickFlag = 0;
	private int rotaryTicks = 0;
	private float speed = 0.0447f;
	private int syncPinState;

	// movement / trial tracking
	private float initialPosition_z = 0.0f;
	private Vector3 initialPosition;
	private Vector3 lastPosition;
	private Vector3 blackoutBoxPosition = new Vector3(0f, -24.0f, 526.0f);
	public int numTraversals = 0;
	private int numTrialsTotal;
	private int numTrialsPerBlock;
	private float current_z;
	private float delta_z;
	private Vector3 rewardZonePos = new Vector3(0f, 1f, 0f);

	public int numTrials = 400;


	//Object1.setActive(true);

	// for calculating sequence of reward locations
	private float pReward;  // probability of reward each trial
	private List<int> rewardTrials; // trial num for each reward
	private List<int> blockType;
	private int rewardCount = 0;
	public List<int> rewardCenters;
	private List<int> automaticRewards;
	//public float[] blockType;

	public int warmupFreeRewards = 20; // How many rewards delivered automatically at the start of the session
	public int rewardMissesBeforeReminder = 3; // How many trials mouse can miss before automatic reminder reward
	public bool requireLickForReward = false;
	private int numMissedRewards = 0;

	// for each trial's reward
	public int rewardTrial;
	public float rewardPosition;
	public float rewardZoneStart;
	public float rewardZoneEnd;
	public float rewardZoneBuffer = 15f;
	private bool lickForReward;
	private int rewardFlag = 0;
	private int automaticRewardFlag; // = 1;


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

	// teleport
	private float teleportPosition;
	private int teleportFlag = 0;
	//teleportPosition = paramsScript.trackEnd;
	private int trackLen;

	public enum ArduinoUse { Uniduino, SimulatedRunning };
	public ArduinoUse ArdUse;
	public float simRunSpeed = 25f;
	[HideInInspector]
	public bool simulatedRunning = false;

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

	private int cmdWrite = 0;


	private void Awake()
	{

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


		_serialPort.DiscardInBuffer();
		_serialPort.DiscardOutBuffer();

		_serialPortReward.DiscardInBuffer();
		_serialPortReward.DiscardOutBuffer();
	}


	void Start ()
	{	
		initialPosition = new Vector3 (0f, 0.5f, initialPosition_z);

		Debug.Log("Handshaking Arduino connection, X to quit");
		bool connectedArd = false;
		while (connectedArd == false)
		{
			cmdWrite = 10;
			_serialPort.Write(cmdWrite.ToString() + ',');
			try
			{
				string shake_check = _serialPort.ReadLine();
				int shake_value = int.Parse(shake_check);
				if (shake_value == 10)
				{
					Debug.Log("Got a 10 back, handshook the arduino");
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
		// initialize arduino
		/*
		try
		{
			connect(port,57600, true, 4);
			Debug.Log("Connected to lick and sync ports");
		}
		catch {
			Debug.Log ("failed to connect to lick and sync ports");
		}
		*/

		//arduino = Arduino.global;
		//arduino.Setup(ConfigurePins);
		//arduino.digitalWrite (stopPin, Arduino.LOW);

		// get session parameters from SessionParams script
		GameObject player = GameObject.Find ("Player");
		paramsScript = player.GetComponent<SessionParams> ();
		numGainTrials = paramsScript.numGainTrials;
		endGain = paramsScript.endGain;
		manipSession = paramsScript.manipSession;

		// track params
		numTrialsTotal = paramsScript.numTrialsTotal;
		numTrialsPerBlock = paramsScript.numTrialsPerBlock;
		initialPosition_z = paramsScript.trackStart;
		teleportPosition = paramsScript.trackEnd;
		trackLen = (int)teleportPosition;
	
		// reminder params
		numReminderTrials = paramsScript.numReminderTrials;
		numReminderTrialsEnd = paramsScript.numReminderTrialsEnd;
		numReminderTrialsBegin = paramsScript.numReminderTrialsBegin;

		// reward params
		lickForReward = paramsScript.lickForReward;
		pReward = paramsScript.pReward;
			
		// for detecting licks
		//lickScript = player.GetComponent<DetectLicks> ();

		// set gain change trials
		gainTrialSequence = new float[numTrialsTotal];
		if (manipSession) {
			for (int i = 0; i < numTrialsTotal - numGainTrials; i++) {
				gainTrialSequence [i] = 1.0f;
			}
			for (int j = 0; j < numGainTrials; j++) {
				gainTrialSequence [j + numTrialsTotal - numGainTrials] = endGain;
			}
		} else {
			for (int i = 0; i < numTrialsTotal; i++) {
				gainTrialSequence [i] = 1.0f;
			}
		}

		// DEFINE REWARD SEQUENCE
		rewardCenters = new List<int>();
		rewardTrials = new List<int>();
		automaticRewards = new List<int>();
		int trial;
		int pos;

		for (int i = 0; i < ((numTrialsTotal - 1) * trackLen); i++) {
			float thresh = UnityEngine.Random.value;
			// give rewards with probability defined by pReward and track length
			// exclude first and last 50cm of track
			if (thresh <= pReward & i % trackLen >= 50 & i % trackLen <= trackLen - 50) {
				trial = i / trackLen;
				pos = i % trackLen;


				// first reward
				if (rewardCenters.Count == 0) {
					rewardCenters.Add (pos);
					rewardTrials.Add (trial);
					if (requireLickForReward == false)
                    {
						automaticRewards.Add(1);
                    }
                    else
                    {
						if (warmupFreeRewards > 0)
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
				if ((trial == rewardTrials [rewardTrials.Count - 1]) & (pos < (rewardCenters [rewardCenters.Count - 1] + 50))) {
					continue;
				}

				// next trial but still within 50cm
				else if ((trial > rewardTrials [rewardTrials.Count - 1]) & ((pos + 400) < (rewardCenters [rewardCenters.Count - 1] + 50))){
					continue;
				}

				// passes criteria
				else {
					rewardCenters.Add(pos);
					rewardTrials.Add(trial);

					if (requireLickForReward == true)
					{
						if (rewardCenters.Count > warmupFreeRewards)
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
		Debug.Log("num reward centers = " + rewardCenters.Count + ", num automatic rewards = " + automaticRewards.Count);

		// initialize reward zone (if any rewards)
		if (rewardCenters.Count > 0) {
			rewardTrial = rewardTrials [0];
			rewardPosition = rewardCenters [0];
			rewardZoneStart = rewardCenters [0] - 25;
			rewardZoneEnd = rewardCenters [0] + 25;
			automaticRewardFlag = automaticRewards[0];
		}
		rewardZonePos[2] = (float)rewardPosition;
		//rewardZonePos[2] = rewardCenters[rewardCount];
		rewardZone.transform.localPosition = rewardZonePos;
		
		// initialize reminder flag
		reminderFlag = numReminderTrials;
			
		// for saving data		
		mouse = paramsScript.mouse;
		session = paramsScript.session;
		saveData = paramsScript.saveData;
		localDirectory = paramsScript.localDirectory;
		serverDirectory = paramsScript.serverDirectory;
		trialTimesFile = localDirectory + "\\" + mouse + "\\" + session + "_trial_times.txt";
		serverTrialTimesFile = serverDirectory + "\\" + mouse + "\\VR\\" + session + "_trial_times.txt"; 
		rewardFile = localDirectory + "\\" + mouse + "\\" + session + "_reward.txt";
		serverRewardFile = serverDirectory + "\\" + mouse + "\\VR\\" + session + "_reward.txt";
		lickFile = localDirectory + "\\" + mouse + "\\" + session + "_licks.txt";
		serverLickFile = serverDirectory + "\\" + mouse + "\\VR\\" + session + "_licks.txt";
		positionFile = localDirectory + "\\" + mouse + "\\" + session + "_position.txt";
		serverPositionFile = serverDirectory + "\\" + mouse + "\\VR\\" + session + "_position.txt";
		startStopFile = localDirectory + "\\" + mouse + "\\" + session + "startstop.txt";


		Debug.Log("Press return to start the session");

	}

	void Update ()
	{
		/*
		if (lastRewardFlag != rewardFlag)
        {
			Debug.Log("Beep")
        }
		*/

		if (recordingStarted == false)
        {
			if (Input.GetKeyDown(KeyCode.Return))
			{
				Debug.Log("starting Update");
				recordingStarted = true;
			}
		}

		if (recordingStarted == true)
		{

			if (sentStartSignal == false)
			{
				cmdWrite = 4;
				_serialPort.Write(cmdWrite.ToString() + ',');
				sentStartSignal = true;
				var sw_startstop = new StreamWriter(startStopFile, true);
				sw_startstop.Write("StartSignal" + "\t" + Time.realtimeSinceStartup + "\n");
				sw_startstop.Close();
				_serialPort.DiscardInBuffer();
				_serialPort.DiscardOutBuffer();
				//pulseMarker = 1;
				Debug.Log("Trial= " + (numTraversals + 1) + ", rewardCount= " + rewardCount + ", reward pos=" + rewardPosition + ", automaticReward= " + automaticRewardFlag);

				_serialPortReward.DiscardInBuffer();
				_serialPortReward.DiscardOutBuffer();
			}

			rewardZonePos = rewardZone.transform.localPosition;
			rewardZonePos[2] = rewardPosition;

			if (numTraversals+1 == rewardTrial) // Only if it's the next trial
			{
				rewardZonePos[2] = rewardPosition + (float)trackLen;
				//Debug.Log(rewardZonePos);
			}
			if (numTraversals+1 < rewardTrial) // Otherwise hide it
            {
				rewardZonePos[2] = rewardPosition - (float)trackLen;
            }
			rewardZone.transform.localPosition = rewardZonePos;

			if (rotaryTicks == 0)
			{
				transform.position = lastPosition;
			}
			else
			{
				float delta_z = rotaryTicks * speed;
				float current_z = transform.position.z;
				transform.position = new Vector3(0.0f, 0.5f, current_z + delta_z);
			}
			lastPosition = transform.position;

			cmdWrite = 1;

			if (Input.GetKeyDown(KeyCode.Space))
			{
				//StartCoroutine( Reward ());
				cmdWrite = 2;
				_serialPort.Write(cmdWrite.ToString() + ',');
				cmdWrite = 1;
			}

			// reset teleport and reward flags in appropriate zone
			int rewardFlagLast = rewardFlag;
			if (rewardTrial == numTraversals & transform.position.z < rewardZoneStart)// - 20
			{
				rewardFlag = 0;
			}

			if (rewardFlagLast != rewardFlag)
			{
				//Debug.Log("setting RF");
			}

			if (transform.position.z < rewardZoneStart - 20)
			{
				teleportFlag = 0;
			}

			// reward
			//if (transform.position.z > rewardZoneStart & 

			//Debug.Log(rewardFlag);
			// always allow animal to lick for reward in zone if box checked
			if (lickForReward & rewardTrial == numTraversals & transform.position.z > rewardZoneStart & transform.position.z < rewardZoneEnd & rewardFlag == 0)
			{
				if (lickFlag == 1)
				{
					rewardFlag = 1;
					//StartCoroutine (Reward ());
					cmdWrite = 2;

					numMissedRewards = 0;
					rewardCount += 1;
					// update debug log and save info
					Debug.Log(lick_raw + "; " + lickFlag);
					Debug.Log("Reward requested " + " Trial= " + (numTraversals + 1) + " lickPinVal= " + lickPinValue + ", rewardCount= " + rewardCount + ", rewardTrial " + rewardTrial + ", reward pos=" + rewardPosition + ", automaticReward= " + automaticRewardFlag + ", num missed rewards= " + numMissedRewards);

					if (saveData)
					{
						var sw = new StreamWriter(rewardFile, true);
						sw.Write(Time.realtimeSinceStartup + "\t" + rewardTrial + "\t" + rewardPosition + "\t" + 0 + "\t" + 0 + "\n");
						sw.Close();
					}

					// update reward zone
					// rewardCount += 1;
					if (rewardCount <= rewardCenters.Count)
					{
						rewardTrial = rewardTrials[rewardCount];
						rewardPosition = rewardCenters[rewardCount];
						rewardZoneStart = rewardPosition - 25;
						rewardZoneEnd = rewardPosition + 25;
						automaticRewardFlag = automaticRewards[rewardCount];
						//Debug.Log("rewardCount" + rewardCount + " automaticRewardFlag " + automaticRewardFlag);
						Debug.Log("rewardCount = " + rewardCount + ", rewardTrial= " + rewardTrial + ", reward pos = " + rewardPosition);
					}
					else
					{ // no more rewards, set zone outside track
						rewardTrial = numTrialsTotal + 1;
						rewardPosition = 800;
						rewardZoneStart = rewardPosition - 25;
						rewardZoneEnd = rewardPosition + 25;
					}


				}
			}

			// automatic reward
			if (automaticRewardFlag == 1 & rewardTrial == numTraversals & transform.position.z > rewardPosition & transform.position.z < rewardZoneEnd & rewardFlag == 0)
			{
				rewardFlag = 1;
				//StartCoroutine (Reward ());
				cmdWrite = 2;
				numMissedRewards = 0;

				rewardCount += 1;
				// update debug log
				Debug.Log("Auto reward delivery " + "Trial= " + (numTraversals + 1) + ", rewardCount= " + rewardCount + ", rewardTrial " + rewardTrial + ", reward pos= " + rewardPosition + ", automaticReward= " + automaticRewardFlag +  ", num missed rewards= " + numMissedRewards);

				if (saveData)
				{
					var sw = new StreamWriter(rewardFile, true);
					sw.Write(Time.realtimeSinceStartup + "\t" + rewardTrial + "\t" + rewardPosition + "\t" + 1 + "\t" + 0 + "\n");
					sw.Close();
				}

				// update reward zone
				//rewardCount += 1;
				if (rewardCount <= rewardCenters.Count)
				{
					rewardTrial = rewardTrials[rewardCount];
					rewardPosition = rewardCenters[rewardCount];
					rewardZoneStart = rewardPosition - 25;
					rewardZoneEnd = rewardPosition + 25;
					automaticRewardFlag = automaticRewards[rewardCount];
					//Debug.Log("rewardCount" + rewardCount + " automaticRewardFlag " + automaticRewardFlag + "num missed rewards= " + numMissedRewards);
					Debug.Log("rewardCount = " + rewardCount + ", rewardTrial= " + rewardTrial + ", reward pos = " + rewardPosition);
				}
				else
				{
					rewardTrial = numTrialsTotal + 1;
					rewardPosition = 800;
					rewardZoneStart = rewardPosition - 25;
					rewardZoneEnd = rewardPosition + 25;
				}
			}

			// missed reward
			if (automaticRewardFlag == 0 & rewardTrial == numTraversals & transform.position.z > rewardZoneEnd & rewardFlag == 0)
            {
				numMissedRewards = numMissedRewards + 1;
				rewardCount += 1;

				// update debug log 
				Debug.Log("Missed Reward, Trial= " + (numTraversals + 1) + " lickPinVal= " + lickPinValue + ", rewardCount= " + rewardCount + ", rewardTrial " + rewardTrial + ", reward pos= " + rewardPosition + ", automaticReward= " + automaticRewardFlag + ", num missed rewards= " + numMissedRewards);

				// update reward zone
				//rewardCount += 1;
				rewardFlag = 1;
				if (rewardCount <= rewardCenters.Count)
				{
					rewardTrial = rewardTrials[rewardCount];
					rewardPosition = rewardCenters[rewardCount];
					rewardZoneStart = rewardPosition - 25;
					rewardZoneEnd = rewardPosition + 25;
					automaticRewardFlag = automaticRewards[rewardCount];
					//Debug.Log("num traversals " + numTraversals + ", rewardCount = " + rewardCount + ", rewardTrial= " + rewardTrial + ", reward pos = " + rewardPosition);
				}
				else
				{
					rewardTrial = numTrialsTotal + 1;
					rewardPosition = 800;
					rewardZoneStart = rewardPosition - 25;
					rewardZoneEnd = rewardPosition + 25;
				}

				
				if (numMissedRewards >= rewardMissesBeforeReminder)
                {
					automaticRewardFlag = 1;
					numMissedRewards = 0;
					Debug.Log("Next trial automatic reward");
                }
				//Debug.Log("rewardCount  " + rewardCount + " automaticRewardFlag  " + automaticRewardFlag);
			}

			if (numTraversals > rewardTrial)
			{
				
				Debug.Log("Reward count error: numTraversals = " + numTraversals + ", rewardTrial = " + rewardTrial + ", rewardCount = " + rewardCount + ", setting reward trial to next ahead");
				while (rewardTrial <= numTraversals && rewardCount <= rewardCenters.Count)
				{
					rewardCount++;
					rewardTrial = rewardTrials[rewardCount];
				}
				if (rewardCount >= rewardCenters.Count)
				{
					Debug.Log("Reward count = " + rewardCount + ", num reward trials = " + rewardTrials.Count + "no more reward trials?");
				}
				else
				{
					rewardPosition = rewardCenters[rewardCount];
					rewardZoneStart = rewardPosition - 25;
					rewardZoneEnd = rewardPosition + 25;
					automaticRewardFlag = 1;
					Debug.Log("Reward count = " + rewardCount + ", rewardTrial = " + rewardTrial + " rewardPos = " + rewardPosition);
				}
			}

			// teleport
			if (transform.position.z > teleportPosition & teleportFlag == 0)
			{

				// update number of trials
				numTraversals += 1;
				teleportFlag = 1;
				reminderFlag -= 1;

				// update reminder flag at end of each block
				if ((numTraversals - numReminderTrialsBegin) % numTrialsPerBlock == 0)
				{
					reminderFlag = numReminderTrials;
				}

				// update reward position for missed rewards
				/*
				while (numTraversals > rewardTrial)
				{

					Debug.Log("Trial " + (numTraversals + 1) + ", reward pos=" + rewardPosition + ", automaticReward=" + automaticRewardFlag + ", missed reward");

					if (saveData)
					{
						var sw = new StreamWriter(rewardFile, true);
						sw.Write(Time.realtimeSinceStartup + "\t" + rewardTrial + "\t" + rewardPosition + "\t" + 0 + "\t" + 1 + "\n");
						sw.Close();
					}

					rewardCount += 1;
					if (rewardCount <= rewardCenters.Count)
					{
						rewardTrial = rewardTrials[rewardCount];
						rewardPosition = rewardCenters[rewardCount];
						rewardZoneStart = rewardPosition - 25;
						rewardZoneEnd = rewardPosition + 25;
					}
					else
					{
						rewardTrial = numTrialsTotal + 1;
						rewardPosition = 800;
						rewardZoneStart = rewardPosition - 25;
						rewardZoneEnd = rewardPosition + 25;
					}
				}
				*/

				// set automaticRewardFlag to 1 if the upcoming trial will be automatic reward
				/*
				if (!lickForReward | reminderFlag > 0 | numTraversals < numReminderTrialsBegin | numTraversals >= numTrialsTotal - numReminderTrialsEnd)
				{
					automaticRewardFlag = 1;
				}
				else
				{
					automaticRewardFlag = 0;
				}
				*/

				// TELEPORT
				transform.position = initialPosition;

				// write trial time to file
				if (numTraversals < numTrialsTotal)
				{
					// update contrast and gain
					gain = gainTrialSequence[numTraversals];

					// update debug log
					Debug.Log("Trial " + (numTraversals + 1) + ", gain = " + gain);

					if (saveData)
					{
						var sw = new StreamWriter(trialTimesFile, true);
						sw.Write(Time.realtimeSinceStartup + "\t" + automaticRewardFlag + "\n");
						sw.Close();
					}
				}
				else
				{
					// end session after appropriate number of trials
					//cmdWrite = 8;
					//_serialPort.Write (cmdWrite.ToString () + ',');
					//StartCoroutine (StopSession ());
					//StartCoroutine(SendEmail()); // send email notification that session is done
					UnityEditor.EditorApplication.isPlaying = false;
				}
			}
			else if (transform.position.z > teleportPosition)
			{
				// catch situations where the mouse fails to teleport
				transform.position = initialPosition;
				Debug.Log("Got to the teleport catch");
			}



			_serialPort.Write(cmdWrite.ToString() + ',');
			int missedArdFrame = 0;
			try
			{
				lick_raw = _serialPort.ReadLine();
				//Debug.Log(lick_raw);
				//if (String.Equals(lick_raw[0], "0"))
				if(lick_raw[0]==0)
				{
					Debug.Log("Bad read");
					lick_raw = string.Concat("1", lick_raw);
				}
				string[] lick_list = lick_raw.Split('\t');
				
				if (lick_list.Length < 4)
				{ 
					Debug.Log("Bad convert");
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
				/*
				if (lickPinValue == 23)
                {
					lickPinValue = 1023;
                }
				if (lickPinValue == 3)
				{
					lickPinValue = 1023;
				}
				*/


				//if (lickPinValue > 500 & lickFlag == 1)
				if (lickPinValue > 500)
				{
					lickFlag = 0;
				}
				//if (lickPinValue < 500 & lickFlag == 0)
				if (lickPinValue < 500)
				{
					numLicks += 1;
					lickFlag = 1;

					if (saveData)
					{
						//var sw_lick = new StreamWriter(lickFile, true);
						//sw_lick.Write(transform.position.z + "\t" + Time.realtimeSinceStartup + "\n");
						//sw_lick.Close();
					}

				}

				rotaryTicks = int.Parse(lick_list[1]);
				syncPinState = int.Parse(lick_list[2]);
				startStopPinState = int.Parse(lick_list[3]);

				//pulseMarker = pulseMarker * -1;
			}
			catch (TimeoutException)
			{
				Debug.Log("lickport timeout on frame " + Time.frameCount);
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

					_serialPortReward.Write(cmdWrite.ToString() + ',');
					//_serialPortReward.DiscardInBuffer();
					//_serialPortReward.DiscardOutBuffer();
				}
				catch
				{
					Debug.Log("failed to write lick command");
				}
			}

			_serialPort.DiscardInBuffer();
			_serialPort.DiscardOutBuffer();

			_serialPortReward.DiscardInBuffer();
			_serialPortReward.DiscardOutBuffer();

			delta_z = rotaryTicks * speed;
			current_z = transform.position.z;
			bool allowMovement = true;
			if (allowMovement == true)
			{
				if (rotaryTicks == 0)
				{
					transform.position = lastPosition;
				}
				else
				{
					//float delta_z = rotaryTicks * speed;
					//float current_z = transform.position.z;
					transform.position = new Vector3(0.0f, 0.5f, current_z + delta_z);
				}

				lastPosition = transform.position;

				if (lastPosition[2] < -10)
				{
					lastPosition[2] = -10;
					transform.position = lastPosition;
					Debug.Log("Caught the camera way out of bounds");
				}
					
				

				
			}
			/*
			_serialPort.Write (cmdWrite.ToString () + ',');
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
						var sw = new StreamWriter(lickFile, true);
						sw.Write(transform.position.z + "\t" + Time.realtimeSinceStartup + "\n");
						sw.Close();
					}
				}

				rotaryTicks = int.Parse(lick_list[1]);

				syncPinState = int.Parse(lick_list[2]);
			}
			catch (TimeoutException) {
				Debug.Log ("lickport timeout");
			}
			*/

			if (saveData)
			{
				int cueOrderPrint = 0;
				float delta_T = Time.deltaTime;
				float delta_z = rotaryTicks * speed;

				var sw_pos = new StreamWriter(positionFile, true);
				//sw_pos.Write(transform.position.z + "\t" + Time.realtimeSinceStartup + "\t" + syncPinState + "\n");
				sw_pos.Write(transform.position.z + "\t" + Time.realtimeSinceStartup + "\t" + syncPinState + "\t" +
									pulseMarker + "\t" + startStopPinState + "\t" + delta_z + "\t" +
									lickPinValue + "\t" + rewardFlag + "\t" + numTraversals + "\t" + cueOrderPrint + "\t" + delta_T + "\t" + missedArdFrame + "\n");
				sw_pos.Close();
			}
		}

	}

	/*
	void ConfigurePins () 
	{
		arduino.pinMode (rewardPin, PinMode.OUTPUT);
		arduino.pinMode (stopPin, PinMode.OUTPUT);
		Debug.Log ("Pins configured (player controller)");
	}
	*/
	/*
	IEnumerator Reward ()
	{
		//arduino.digitalWrite (rewardPin, Arduino.HIGH);
		//yield return new WaitForSeconds (0.02f);
		//arduino.digitalWrite (rewardPin, Arduino.LOW);
	}
	*/
	/*
	IEnumerator StopSession()
	{
		//arduino.digitalWrite (stopPin, Arduino.HIGH);
		//yield return new WaitForSeconds (0.1f);
		//arduino.digitalWrite (stopPin, Arduino.LOW);

		StartCoroutine (SendEmail ()); // send email notification that session is done
		UnityEditor.EditorApplication.isPlaying = false;
	}		
	*/

	IEnumerator SendEmail()
	{
		MailMessage mail = new MailMessage ();

		mail.From = new MailAddress ("giocomo.lab.vr.rig@gmail.com");
		mail.To.Add ("ilow@stanford.edu");
        mail.Subject = "VR session complete: Isabel rig";
        mail.Body = "";

		SmtpClient smtpServer = new SmtpClient ("smtp.gmail.com");
		smtpServer.Port = 587;
		smtpServer.Credentials = new System.Net.NetworkCredential ("giocomo.lab.vr.rig", "entorhinal!1234") as ICredentialsByHost;
		smtpServer.EnableSsl = true;
		ServicePointManager.ServerCertificateValidationCallback = 
		delegate(object s, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) {
				return true;
		};
		smtpServer.Send (mail);

		yield return null;
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
	}

	private void Disconnect()
	{
		Close ();		
	}

	void OnDestroy()
	{				
		Disconnect();
	}

	// save trial data to server
	void OnApplicationQuit ()
	{
		
		Debug.Log("Quitting");
		Debug.Log("Stopping 2 " + Time.realtimeSinceStartup);
		cmdWrite = 4;
		_serialPort.Write(cmdWrite.ToString() + ',');
		sw_startstop.Write("StopSignalSent" + "\t" + Time.realtimeSinceStartup + "\n");

		if (saveData) {
			File.Copy (trialTimesFile,serverTrialTimesFile);
			File.Copy (rewardFile,serverRewardFile);
			File.Copy(lickFile, serverLickFile);
			File.Copy(positionFile, serverPositionFile);
		}
	}
	/*
	void OnApplicationQuit()
	{
		Debug.Log("Quitting");
		Debug.Log("Stopping 2 " + Time.realtimeSinceStartup);
		cmdWrite = 4;
		_serialPort.Write(cmdWrite.ToString() + ',');
		sw_startstop.Write("StopSignalSent" + "\t" + Time.realtimeSinceStartup + "\n");

		sw_startstop.Close();
		sw_trial.Close();
		sw_pos.Close();
		sw_reward.Close();
		sw_lick.Close();

		if (saveData)
		{
			File.Copy(trialTimesFile, serverTrialTimesFile);
			File.Copy(rewardFile, serverRewardFile);
			File.Copy(lickFile, serverLickFile);
			File.Copy(positionFile, serverPositionFile);
		}
	}
	*/


}
