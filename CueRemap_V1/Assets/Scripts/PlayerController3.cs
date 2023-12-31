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

public class PlayerController3 : MonoBehaviour 
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
	private Vector3 blackoutBoxPosition = new Vector3( 0f, -24.0f, 526.0f );
	public int numTraversals = 0;
	//private int numTrialsTotal;
	private int numTrialsPerBlock;

	// Remapping stuff
	public int numBaselineTrials = 150;
	public int numRemapTrials = 150;
	public int numReinstatementTrials = 100;
	public int numTrialsTotal;
	public float baselineRewardLocRel = 0.6f;
	public float remapRewardLocRel = 0.6f;
	public float reinstateRewardLocRel = 0.6f;

	public int[] baselineCueOrder = { 1, 2, 3, 4, 5 };
	public int[] remapCueOrder = { 3, 4, 1, 5, 2 }; // {3, 1, 5, 2, 4} 4 2 5 1 3
	public float[] objectPositionsRel = { 0.1f, 0.23f, 0.5f, 0.78f, 0.9f };
	public GameObject Object1;
	public GameObject Object2;
	public GameObject Object3;
	public GameObject Object4;
	public GameObject Object5;
	private List<GameObject> ObjectCues = new List<GameObject>();

	//Object1.setActive(true);

	// for calculating sequence of reward locations
	private float pReward;  // probability of reward each trial
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

	// teleport
	private float teleportPosition;
	//private int teleportFlag = 0;
	//teleportPosition = paramsScript.trackEnd;
	private int trackLen;
	private bool justTeleported = false;

	public enum ArduinoUse { Uniduino, SimulatedRunning};
	public ArduinoUse ArdUse;
	public float simRunSpeed = 25f;
	[HideInInspector]
	public bool simulatedRunning = false;

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

	private void Awake()
    {
		Debug.Log("wake");
		// initialize arduino
		if (ArdUse == ArduinoUse.Uniduino)
		{
			/*
			arduino = Arduino.global;
			arduino.Setup(ConfigurePins);
			arduino.digitalWrite(stopPin, Arduino.LOW);
			*/
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
		Debug.Log("Sim run pc3: " + simulatedRunning);

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
		_serialPort.DiscardInBuffer();
		_serialPort.DiscardOutBuffer();

		_serialPortReward.DiscardInBuffer();
		_serialPortReward.DiscardOutBuffer();

		

	}

	void Start ()
	{
		Debug.Log("PC3 start");
		initialPosition = new Vector3 (0.0f, 0.5f, initialPosition_z);

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

		

		/*
		// initialize arduino
		if (ArdUse == ArduinoUse.Uniduino)
		{
			arduino = Arduino.global;
			arduino.Setup(ConfigurePins);
			arduino.digitalWrite(stopPin, Arduino.LOW);
		}
        else
        {
			Debug.Log("*Configuring fake pins*");
			simulatedRunning = true;
        }
		Debug.Log("Sim run pc3: " + simulatedRunning);
		*/

		// get session parameters from SessionParams script
		GameObject player = GameObject.Find ("Player");
		paramsScript = player.GetComponent<SessionParams> ();
		numGainTrials = paramsScript.numGainTrials;
		endGain = paramsScript.endGain;
		manipSession = paramsScript.manipSession;

		// track params
		numTrialsTotal = numBaselineTrials + numRemapTrials + numReinstatementTrials;
		Debug.Log("num trials total PC3: " + numTrialsTotal);
		//numTrialsTotal = paramsScript.numTrialsTotal;
		numTrialsPerBlock = paramsScript.numTrialsPerBlock;
		initialPosition_z = paramsScript.trackStart;
		teleportPosition = paramsScript.trackEnd;
		//int trackLen = (int)teleportPosition;
		trackLen = (int)teleportPosition;
		transform.position = initialPosition;

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
		//rewardCenters = new List<int>();
		rewardTrials = new List<int> ();
		//int trial;
		//int pos;

		rewardCenters = new float[numTrialsTotal];
		blockType = new List<int>();
		rewardTrials = new List<int>();

		for (int i = 0; i < numTrialsTotal; i++)
        {
			rewardTrials.Add(i);
			if (i < numBaselineTrials)
            {
				rewardCenters[i] = baselineRewardLocRel * trackLen;
				blockType.Add(1);
            }
			else
            {
				if (i < (numBaselineTrials+numRemapTrials) )
                {
					rewardCenters[i] = remapRewardLocRel * trackLen;
					blockType.Add(2);
				}
                else
                {
					rewardCenters[i] = reinstateRewardLocRel * trackLen;
					blockType.Add(3);
				}
            }
        }
		SetObjectPositions();

		/*
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
				}
			}
		}
		*/

		// initialize reward zone (if any rewards)
		//if (rewardCenters.Count > 0) {
		rewardTrial = rewardTrials [0];
		rewardPosition = rewardCenters [0];
		rewardZoneStart = rewardCenters [0] - rewardZoneBuffer;
		rewardZoneEnd = rewardCenters [0] + rewardZoneBuffer;
		//}

		// initialize reminder flag
		reminderFlag = numReminderTrials;
			
		// for saving data		
		mouse = paramsScript.mouse;
		session = paramsScript.session;
		saveData = paramsScript.saveData;
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

		//Debug.Log("1");

		sw_lick = new StreamWriter(lickFile, true);
		sw_pos = new StreamWriter(positionFile, true);
		sw_trial = new StreamWriter(trialTimesFile, true);
		sw_reward = new StreamWriter(rewardFile, true);
		sw_startstop = new StreamWriter(startStopFile, true);
		sw_par = new StreamWriter(paramsTwoFile, true);

		
		sw_par.WriteLine("numBaselineTrials\t" + numBaselineTrials);
		sw_par.WriteLine("numRemapTrials\t" + numRemapTrials);
		sw_par.WriteLine("numReinstatementTrials\t" + numReinstatementTrials);
		try
        {
			string borderresult = " ";
			foreach (var item in baselineCueOrder)
			{
				borderresult += item.ToString() + ", ";
			}
			sw_par.WriteLine("Baseline Order\t" + borderresult);
			string rorderresult = " ";
			foreach (var item in remapCueOrder)
			{
				rorderresult += item.ToString() + ", ";
			}
			sw_par.WriteLine("Remap Order\t" + rorderresult);
			string relresult = " ";
			foreach (var item in objectPositionsRel)
			{
				relresult += item.ToString() + ", ";
			}
			sw_par.WriteLine("objectPositionsRel\t" + relresult);
			string ob1name = Object1.name;
			string ob2name = Object2.name;
			string ob3name = Object3.name;
			string ob4name = Object4.name;
			string ob5name = Object5.name;
			sw_par.WriteLine("Object1 name\t" + ob1name);
			sw_par.WriteLine("Object2 name\t" + ob2name);
			sw_par.WriteLine("Object3 name\t" + ob3name);
			sw_par.WriteLine("Object4 name\t" + ob4name);
			sw_par.WriteLine("Object5 name\t" + ob5name);
		}
		catch
        {
			Debug.Log("failed to record some param2");
        }
		sw_par.Close();

		//Debug.Log("2");

		/*
		bool pressedSpace = false;
		bool dispSpace = false;
		while (pressedSpace == false)
        {
			if (dispSpace == false)
            {
				Debug.Log("Press Enter/Return to start running");
			}

			if (Input.GetKeyDown(KeyCode.Return))
			{
				Debug.Log("starting Update");
				pressedSpace = true;
			}
		}
		*/

		//float delayTime = 3.0f;
		//WaitForSeconds(delayTime);
		// Nope, has to be in an IEnumerator function

		Debug.Log("Press return to start the session");

		if (Application.targetFrameRate != target)
			Application.targetFrameRate = target;
		
	}

	void Update()
	{
		if (Application.targetFrameRate != target)
			Application.targetFrameRate = target;

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
				sw_startstop.Write("StartSignal" + "\t" + Time.realtimeSinceStartup + "\n");
				_serialPort.DiscardInBuffer();
				_serialPort.DiscardOutBuffer();
				//pulseMarker = 1;

				_serialPortReward.DiscardInBuffer();
				_serialPortReward.DiscardOutBuffer();
			}

			cmdWrite = 1;


			if (Input.GetKeyDown(KeyCode.Space))
			{
				//StartCoroutine( Reward ());
				cmdWrite = 2;
				_serialPort.Write(cmdWrite.ToString() + ',');
				cmdWrite = 1;
			}


			// reset teleport and reward flags in appropriate zone
			if (rewardTrial == numTraversals & transform.position.z < rewardZoneStart - 20)
			{
				rewardFlag = 0;
			}
			if (transform.position.z < rewardZoneStart - 20)
			{
				//teleportFlag = 0;
			}

			// reward
			// always allow animal to lick for reward in zone if box checked
			//if (lickForReward & rewardTrial == numTraversals & transform.position.z > rewardZoneStart & transform.position.z < rewardZoneEnd & rewardFlag == 0)
			if (lickForReward & transform.position.z > rewardZoneStart & transform.position.z < rewardZoneEnd & rewardFlag == 0)
			{
				if (lickFlag == 1)
				{
					rewardFlag = 1;
					//StartCoroutine(Reward());
					cmdWrite = 2;

					// update debug log and save info
					Debug.Log("Trial " + (numTraversals + 1) + ", reward pos=" + rewardPosition + ", automaticReward=" + automaticRewardFlag + ", reward requested");

					if (saveData)
					{
						//var sw = new StreamWriter(rewardFile, true);
						sw_reward.Write(Time.realtimeSinceStartup + "\t" + rewardTrial + "\t" + rewardPosition + "\t" + 0 + "\t" + 0 + "\n");
						//sw_reward.Close();
					}

					// update reward zone
					rewardCount += 1;
					if (rewardCount <= rewardCenters.Length)
					{
						rewardTrial = rewardTrials[rewardCount];
						rewardPosition = rewardCenters[rewardCount];
						rewardZoneStart = rewardPosition - rewardZoneBuffer;
						rewardZoneEnd = rewardPosition + rewardZoneBuffer;
					}
					/*
					} else { // no more rewards, set zone outside track
						rewardTrial = numTrialsTotal + 1;
						rewardPosition = 800;
						rewardZoneStart = rewardPosition - 25;
						rewardZoneEnd = rewardPosition + 25;
					}
					*/


				}
			}


			// automatic reward
			//if (automaticRewardFlag == 1 & rewardTrial == numTraversals & transform.position.z > rewardPosition & rewardFlag == 0)
			if (automaticRewardFlag == 1 & transform.position.z > rewardPosition & rewardFlag == 0)
			{
				rewardFlag = 1;
				//StartCoroutine(Reward());
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
				//Debug.Log(rewardCount + ";; " + rewardCenters.Length);
				if (rewardCount < rewardCenters.Length)
				{
					rewardTrial = rewardTrials[rewardCount];
					rewardPosition = rewardCenters[rewardCount];
					rewardZoneStart = rewardPosition - rewardZoneBuffer;
					rewardZoneEnd = rewardPosition + rewardZoneBuffer;
				}
				/*
				} else { // no more rewards, set zone outside track
					rewardTrial = numTrialsTotal + 1;
					rewardPosition = 800;
					rewardZoneStart = rewardPosition - 25;
					rewardZoneEnd = rewardPosition + 25;
				}
				*/
			}



			// teleport
			if ((transform.position.z > teleportPosition)) // | (transform.position.z < timeoutLimit)
			{
				allowLapEnd = true;
				//Debug.Log(UseTimeout);
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
						Debug.Log(Time.realtimeSinceStartup + ", " + timeoutOver);
						try
                        {
							Debug.Log("This was at " + System.DateTime.Now);
                        }
						catch
                        {

                        }
					}

					float timeNow = Time.realtimeSinceStartup;
					//Debug.Log(timeNow + ", " + timeoutOver);
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
					//teleportFlag = 1;
					//reminderFlag -= 1;
					

					// update reminder flag at end of each block
					//if ((numTraversals - numReminderTrialsBegin) % numTrialsPerBlock == 0)
					//%{
					//	reminderFlag = numReminderTrials;


					// set automaticRewardFlag to 1 if the upcoming trial will be automatic reward
					if (!lickForReward | reminderFlag > 0 | numTraversals < numReminderTrialsBegin | numTraversals >= numTrialsTotal - numReminderTrialsEnd)
					{
						automaticRewardFlag = 1;
					}
					else
					{
						automaticRewardFlag = 0;
					}

					// TELEPORT
					transform.position = initialPosition;
					lastPosition = transform.position;
					justTeleported = true;

					rewardFlag = 0;

					// write trial time to file
					if (numTraversals < numTrialsTotal)
					{
						// Update cue object positions
						SetObjectPositions();

						// update contrast and gain
						gain = gainTrialSequence[numTraversals];

						// update debug log
						Debug.Log("Trial " + (numTraversals + 1) + ", gain = " + gain + ", reward position = " + rewardCenters[rewardCount]);

						if (saveData)
						{
							//var sw = new StreamWriter(trialTimesFile, true);
							sw_trial.Write(Time.realtimeSinceStartup + "\t" + automaticRewardFlag + "\t" + numTraversals + "\t" + blockType[numTraversals] + "\n");
							//
						}
					}
					else
					{
						// end session after appropriate number of trials
						triggerStopSession = true;
						/*
						Debug.Log("Stopping session");
						//cmdWrite = 8;
						//_serialPort.Write (cmdWrite.ToString () + ',');
						//StartCoroutine (StopSession ());
						_serialPort.Write(6.ToString() + ',');
						StartCoroutine(SendEmail()); // send email notification that session is done
						UnityEditor.EditorApplication.isPlaying = false;
						*/
					}

					//}
				}

			}

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

				//pulseMarker = pulseMarker * -1;
			}
			catch (TimeoutException)
			{
				Debug.Log("lickport timeout on frame " + Time.frameCount);
			}
			catch
			{
				Debug.Log("cmd " + cmdWrite + " reply " + lick_raw + " " + Time.frameCount);
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
			

			/*
			float rotaryTickF = UnityEngine.Random.Range(2f, 30f);
			rotaryTicks = (int)rotaryTickF;

			_serialPort.Write(rotaryTicks.ToString() + ',');
			try
			{
				lick_raw = _serialPort.ReadLine();
				string[] lick_list = lick_raw.Split('\t');
				lickPinValue = int.Parse(lick_list[1]);
				Debug.Log("sent, received, at " + rotaryTicks + " " + lickPinValue + " " + Time.realtimeSinceStartup);
			}
			catch (TimeoutException)
			{
				Debug.Log("lickport timeout on frame " + Time.frameCount);
			}
			*/
			_serialPort.DiscardInBuffer();
			_serialPort.DiscardOutBuffer();

			_serialPortReward.DiscardInBuffer();
			_serialPortReward.DiscardOutBuffer();

			if (justTeleported == true)
            {
				rotaryTicks = 0;
				justTeleported = false;
            }

			float delta_z = rotaryTicks * speed;
			float current_z = transform.position.z;
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
			}

			//var sw_pos = new StreamWriter(positionFile, true);
			sw_pos.Write(transform.position.z + "\t" + Time.realtimeSinceStartup + "\t" + syncPinState + "\t" + pulseMarker + "\t" + startStopPinState + "\t" + delta_z + "\t" + lickPinValue + "\n");
			//sw_pos.Write(transform.position.z + "\t" + Time.realtimeSinceStartup + "\t" + syncPinState + "\n");
			//sw_pos.Close();

			pulseMarker = pulseMarker * -1;

			if (triggerStopSession == true)
			{
				Debug.Log("Stopping session at " + Time.realtimeSinceStartup + "\n");
				//cmdWrite = 8;
				//_serialPort.Write (cmdWrite.ToString () + ',');
				//StartCoroutine (StopSession ());
				//cmdWrite = 4;
				//_serialPort.Write(cmdWrite.ToString() + ',');
				//StartCoroutine(SendEmail()); // send email notification that session is done
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

	void SetObjectPositions ()
    {

		//Debug.Log("nObjPosRel :" + objectPositionsRel.Length);
		int relObjPos = 0;
		for (int i=0; i < 5; i++)
        {
			switch (blockType[numTraversals])
            {
				case 1:
					relObjPos = baselineCueOrder [i];
					break;
				case 2:
					relObjPos = remapCueOrder [i];
					break;
				case 3:
					relObjPos = baselineCueOrder[i];
					break;
			}

			
			float relZpos = objectPositionsRel[relObjPos - 1];
			float targetZpos = (float)trackLen * relZpos;
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

}
