using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
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
	// arduino
	public string port = "COM4"; // would probably be helpful to switch this to an enum list
	private SerialPort _serialPort;
	private int delay;
	private string lick_raw;
	private int cmd = 0;

	// data from arduino
	private int rotaryTicks;
	private int numLicks;
	private bool rewardGiven = false;


	// movement / trial tracking
	private float initialPosition_z;
	private Vector3 lastPosition;
	private Vector3 initialPosition;
	public int numTraversals = 0;
	private int numTrialsTotal;
	private int numTrialsPerBlock;
	private float speed = 0.0447f; // makes 1 VR unit = 1 cm for 18 inch circumference cylinder
								   //private float originalSpeed = speed;

	// for calculating sequence of reward locations
	private float pReward;  // probability of reward each trial
	private List<int> rewardTrials; // trial num for each reward
	private List<int> rewardCenters; // position for each reward
	private int rewardCount = 0;

	// for each trial's reward
	public int rewardTrial;
	public int rewardPosition;
	public int rewardZoneStart;
	public int rewardZoneEnd;
	private bool lickForReward;
	private int rewardFlag = 0;
	private int automaticRewardFlag = 1;
	public float rewardZoneBuffer = 20.0f;


	// teleport
	private float teleportPosition;
	private int teleportFlag = 0;

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

	// For visualizing some issues
	public int nextRewardLoc;
	public int nextRewardLoc2;
	public int nextRewardLoc3;


	void Start()
	{
		initialPosition = new Vector3(0f, 0.5f, initialPosition_z);

		// initialize arduino
		connect(port, 115200, true, 4);
		Debug.Log("Connected to lick detector serial port");
		// do something with stop pin?
		//arduino.digitalWrite (stopPin, Arduino.LOW);

		// get session parameters from SessionParams script
		GameObject player = GameObject.Find("Player");
		paramsScript = player.GetComponent<SessionParams>();
		numGainTrials = paramsScript.numGainTrials;
		endGain = paramsScript.endGain;
		manipSession = paramsScript.manipSession;

		// track params
		numTrialsTotal = paramsScript.numTrialsTotal;
		numTrialsPerBlock = paramsScript.numTrialsPerBlock;
		initialPosition_z = paramsScript.trackStart;
		teleportPosition = paramsScript.trackEnd;
		int trackLen = (int)teleportPosition;

		// reminder params
		numReminderTrials = paramsScript.numReminderTrials;
		numReminderTrialsEnd = paramsScript.numReminderTrialsEnd;
		numReminderTrialsBegin = paramsScript.numReminderTrialsBegin;

		// reward params
		lickForReward = paramsScript.lickForReward;
		pReward = paramsScript.pReward;

		// set gain change trials
		gainTrialSequence = new float[numTrialsTotal];
		if (manipSession) {
			for (int i = 0; i < numTrialsTotal - numGainTrials; i++) {
				gainTrialSequence[i] = 1.0f;
			}
			for (int j = 0; j < numGainTrials; j++) {
				gainTrialSequence[j + numTrialsTotal - numGainTrials] = endGain;
			}
		} else {
			for (int i = 0; i < numTrialsTotal; i++) {
				gainTrialSequence[i] = 1.0f;
			}
		}

		// DEFINE REWARD SEQUENCE
		rewardCenters = new List<int>();
		rewardTrials = new List<int>();
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
					rewardCenters.Add(pos);
					rewardTrials.Add(trial);
				}

				// following rewards must be at least 50cm away
				// same trial:
				if ((trial == rewardTrials[rewardTrials.Count - 1]) & (pos < (rewardCenters[rewardCenters.Count - 1] + 50))) {
					continue;
				}

				// next trial but still within 50cm
				else if ((trial > rewardTrials[rewardTrials.Count - 1]) & ((pos + 400) < (rewardCenters[rewardCenters.Count - 1] + 50))) {
					continue;
				}

				// passes criteria
				else {
					rewardCenters.Add(pos);
					rewardTrials.Add(trial);
				}
			}
		}

		// initialize reward zone (if any rewards)
		if (rewardCenters.Count > 0) {
			rewardTrial = rewardTrials[0];
			rewardPosition = rewardCenters[0];
			rewardZoneStart = rewardCenters[0] - 25;
			rewardZoneEnd = rewardCenters[0] + 25;
		}

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
	}

	void Update()
	{
		
		if (Input.GetKeyDown(KeyCode.Space))
		{
			// Free reward at Start... might have to think about how to do this...
			//StartCoroutine( Reward ());
			// Does this stall it until button pressed?
			cmd = 2;

			// flicker the start stop ttl...
		}

		// Read data from arduino
		rotaryTicks = 0;
		numLicks = 0;
		//rewardGiven = fasle; // maybe don't update this here...
		_serialPort.Write(cmd.ToString() + ',');
		try
		{
			lick_raw = _serialPort.ReadLine();
			string[] lick_list = lick_raw.Split('\t');
			rotaryTicks = int.Parse(lick_list[0]);
			numLicks = int.Parse(lick_list[1]);
			//rewardInd = .Parse(lick_list[2]);
			rewardGiven = int.Parse(lick_list[2]) > 0;

		}
		catch (TimeoutException)
		{
			Debug.Log("lickport timeout");
		}

		// Update position
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

		// This is for visualizing variables to help troubleshoot
		nextRewardLoc = rewardCenters[rewardCount];
		try {
			nextRewardLoc2 = rewardCenters[rewardCount + 1];
		}
		catch { }
		try
		{
			nextRewardLoc3 = rewardCenters[rewardCount + 2];
		}
		catch { }

		// reset teleport and reward flags in appropriate zone
		if (rewardTrial == numTraversals & transform.position.z < rewardZoneStart-rewardZoneBuffer)
		{
			rewardFlag = 0;
		}
		if (transform.position.z < rewardZoneStart - rewardZoneBuffer) {
			teleportFlag = 0;
		}

		cmd = 0;
		// reward
		// always allow animal to lick for reward in zone if box checked
		// If the animal is in the reward zone on a rewarded trial
		if (lickForReward & rewardTrial == numTraversals & transform.position.z > rewardZoneStart & transform.position.z < rewardZoneEnd & rewardFlag == 0) {
			if (numLicks > 0) {
				rewardFlag = 1;
				cmd = 2;
				//StartCoroutine (Reward ());

				// update debug log and save info
				Debug.Log ("Trial " + (numTraversals + 1) + ", reward pos=" + rewardPosition + ", automaticReward=" + automaticRewardFlag + ", reward requested");

				if (saveData) {
					var sw = new StreamWriter (rewardFile, true);
					sw.Write (Time.realtimeSinceStartup + "\t" + rewardTrial + "\t" + rewardPosition + "\t" + 0 + "\t" + 0 + "\n");
					sw.Close ();

					sw = new StreamWriter(lickFile, true);
					sw.Write(transform.position.z + "\t" + Time.realtimeSinceStartup + "\n");
					sw.Close();
				}

				// update reward zone
				rewardCount += 1;
				if (rewardCount <= rewardCenters.Count) {
					rewardTrial = rewardTrials [rewardCount];
					rewardPosition = rewardCenters [rewardCount];
				} else { // no more rewards, set zone outside track
					rewardTrial = numTrialsTotal + 1;
					rewardPosition = 800;
				}
				rewardZoneStart = rewardPosition - 25;
				rewardZoneEnd = rewardPosition + 25;

			}
		}

		// automatic reward
		if (automaticRewardFlag == 1 & rewardTrial == numTraversals & transform.position.z > rewardPosition & rewardFlag == 0) {
			rewardFlag = 1;
			cmd = 2;
			//StartCoroutine (Reward ());

			// update debug log
			Debug.Log ("Trial " + (numTraversals + 1) + ", reward pos=" + rewardPosition + ", automaticReward=" + automaticRewardFlag + ", auto reward delivery");

			if (saveData) {
				var sw = new StreamWriter (rewardFile, true);
				sw.Write (Time.realtimeSinceStartup + "\t" + rewardTrial + "\t" + rewardPosition + "\t" + 1 + "\t" + 0 + "\n");
				sw.Close ();
			}

			// update reward zone
			rewardCount += 1;
			if (rewardCount <= rewardCenters.Count) {
				rewardTrial = rewardTrials [rewardCount];
				rewardPosition = rewardCenters [rewardCount];
			} else {
				rewardTrial = numTrialsTotal + 1;
				rewardPosition = 800;
			}
			rewardZoneStart = rewardPosition - 25;
			rewardZoneEnd = rewardPosition + 25;
		}


		// teleport
		if (transform.position.z > teleportPosition & teleportFlag == 0) {

			// update number of trials
			numTraversals += 1;
			teleportFlag = 1;
			reminderFlag -= 1;

			// update reminder flag at end of each block
			if ((numTraversals - numReminderTrialsBegin) % numTrialsPerBlock == 0) {
				reminderFlag = numReminderTrials; 
			}

			// update reward position for missed rewards
			while (numTraversals > rewardTrial) {
				
				Debug.Log ("Trial " + (numTraversals + 1) + ", reward pos=" + rewardPosition + ", automaticReward=" + automaticRewardFlag + ", missed reward");

				if (saveData) {
					var sw = new StreamWriter (rewardFile, true);
					sw.Write (Time.realtimeSinceStartup + "\t" + rewardTrial + "\t" + rewardPosition + "\t" + 0 + "\t" + 1 + "\n");
					sw.Close ();
				}

				rewardCount += 1;
				if (rewardCount <= rewardCenters.Count) {
					rewardTrial = rewardTrials [rewardCount];
					rewardPosition = rewardCenters [rewardCount];
					
				} else {
					rewardTrial = numTrialsTotal + 1;
					rewardPosition = 800;
				}
				rewardZoneStart = rewardPosition - 25;
				rewardZoneEnd = rewardPosition + 25;
			}

			// set automaticRewardFlag to 1 if the upcoming trial will be automatic reward
			if (!lickForReward | reminderFlag > 0 | numTraversals < numReminderTrialsBegin | numTraversals >= numTrialsTotal - numReminderTrialsEnd) {
				automaticRewardFlag = 1;
			} else {
				automaticRewardFlag = 0;
			}

			// TELEPORT
			transform.position = initialPosition;

			// write trial time to file
			if (numTraversals < numTrialsTotal) {
				// update contrast and gain
				gain = gainTrialSequence[numTraversals];

				// update debug log
				Debug.Log ("Trial " + (numTraversals + 1) + ", gain = " + gain);

				if (saveData) {
					var sw = new StreamWriter (trialTimesFile, true);
					sw.Write (Time.realtimeSinceStartup + "\t" + automaticRewardFlag + "\n");
					sw.Close ();
				}
			} else {
				// end session after appropriate number of trials
				StartCoroutine (StopSession ());
				// Flicker the stop pin
				//arduino.digitalWrite (stopPin, Arduino.HIGH);
				//yield return new WaitForSeconds (0.1f);
				//arduino.digitalWrite (stopPin, Arduino.LOW);
			}
		} else if (transform.position.z > teleportPosition) {
			// catch situations where the mouse fails to teleport
			transform.position = initialPosition;
			Debug.Log ("Got to the teleport catch");
		}
	}


	IEnumerator StopSession()
	{
		//arduino.digitalWrite (stopPin, Arduino.HIGH);
		//yield return new WaitForSeconds (0.1f);
		//arduino.digitalWrite (stopPin, Arduino.LOW);
		//StartCoroutine (SendEmail ()); // send email notification that session is done
		cmd = 6;
		_serialPort.Write(cmd.ToString() + ',');
		UnityEditor.EditorApplication.isPlaying = false;
		yield return null;
	}			

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

	// save trial data to server
	void OnApplicationQuit ()
	{
		if (saveData) {
			File.Copy (trialTimesFile,serverTrialTimesFile);
			File.Copy(rewardFile, serverRewardFile);
			File.Copy(lickFile, serverLickFile);
		}
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
		_serialPort.ReadTimeout = 1000; // since on windows we *cannot* have a separate read thread
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

}
