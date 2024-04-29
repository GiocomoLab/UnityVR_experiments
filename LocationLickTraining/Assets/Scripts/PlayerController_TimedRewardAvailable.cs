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

public class PlayerController_TimedRewardAvailable : MonoBehaviour
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

	private float currentTime;
	public enum ArduinoUse { Uniduino, SimulatedRunning };
	public ArduinoUse ArdUse;
	[HideInInspector]
	public bool simulatedRunning = false;
	public float simulatedRunningSpeed = 75.0f;

	public int trackUse = 1;

	public int lickPinValue;

	public float lastRewardTime;
	public float timeBetweenRewards = 3.0f;
	public float nextRewardAvailable;
	public float nextFreeRewardAvailable;

	public bool requireLickForReward = true;
	public int totalFreeRewards = 10;
	public float timeBetweenFreeRewards = 60.0f;

	private int cameraViewDist = 165;
	public int cameraDistReadOnly;

	// movement / trial tracking
	private Vector3 initialPosition;
	private Vector3 lastPosition;
	private Vector3 blackoutBoxPosition;
	
	private string lick_raw;
	
	private int numLicks = 0;
	public int lickFlag = 0;
	
	private int syncPinState;
	
	private float delta_T;
	private float delta_z; 

	private int rewardFlag = 0;
	public int rewardCount;
	public int freeRewardCount;
	
	public int numRewardsTotal = 100;
	
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
	private string positionFile;
	private string serverPositionFile;
	private string startStopFile;
	private string serverStartStopFile;
	private string paramsTwoFile;
	private string serverParamsTwoFile;
	private string trialListFile;
	private string serverTrialListFile;

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

	private int numTraversals;
	private int trackLen = 276;

	private void Awake()
	{
		Debug.Log("wake");

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

		Debug.Log("num trials total: " + numRewardsTotal);

		blackoutBoxPosition = new Vector3(-400.0f, -20.0f, (float)trackLen);
		transform.position = blackoutBoxPosition;

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
			rewardFile = localDirectory + "\\" + mouse + "\\" + session + "_reward.txt";
			serverRewardFile = serverDirectory + "\\" + mouse + "\\" + session + "_reward.txt";
			positionFile = localDirectory + "\\" + mouse + "\\" + session + "_position.txt";
			serverPositionFile = serverDirectory + "\\" + mouse + "\\" + session + "_position.txt";
			lickFile = localDirectory + "\\" + mouse + "\\" + session + "_licks.txt";
			serverLickFile = serverDirectory + "\\" + mouse + "\\" + session + "_licks.txt";
			startStopFile = localDirectory + "\\" + mouse + "\\" + session + "_startStop.txt";
			serverStartStopFile = serverDirectory + "\\" + mouse + "\\" + session + "_startStop.txt";
			paramsTwoFile = localDirectory + "\\" + mouse + "\\" + session + "_params2.txt";
			serverParamsTwoFile = serverDirectory + "\\" + mouse + "\\" + session + "_params2.txt";
			trialListFile = localDirectory + "\\" + mouse + "\\" + session + "_trialList.txt";
			serverTrialListFile = serverDirectory + "\\" + mouse + "\\" + session + "_trialList.txt";

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

		}
		else
		{
			//Debug.Log("Warning: this is NOT saving any data");
		}

		Debug.Log("Press return to start the session");

		if (Application.targetFrameRate != target)
			Application.targetFrameRate = target;

		lastRewardTime = Time.deltaTime;
		nextRewardAvailable = lastRewardTime;
		nextFreeRewardAvailable = lastRewardTime + timeBetweenFreeRewards;
	}

	void Update()
	{
		cameraDistReadOnly = cameraViewDist;

		//missedArdFrame = 0;
		if (Application.targetFrameRate != target)
			Application.targetFrameRate = target;

		if (recordingStarted == false)
		{
			if (Input.GetKeyDown(KeyCode.Return))
			{
				Debug.Log("starting Update");
				recordingStarted = true;
			}

			cmdWrite = 1;
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
					if (lickPinValue > 500)
					{
						lickFlag = 0;
					}
					//if (lickPinValue < 500 & lickFlag == 0)
					if (lickPinValue <= 500)
					{
						numLicks += 1;
						lickFlag = 1;
					}
				}
				catch (TimeoutException)
				{
					Debug.Log("lickport/encoder timeout on frame " + Time.frameCount);
				}
				catch
				{
					Debug.Log("cmd " + cmdWrite + " reply " + lick_raw + " frame " + Time.frameCount);
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
			}

			cmdWrite = 1;

			// Reward
			currentTime = Time.time;
			if (currentTime > nextRewardAvailable)
            {
				if (lickFlag == 1)
                {
					cmdWrite = 2;
					
					lastRewardTime = currentTime;
					nextRewardAvailable = currentTime + timeBetweenRewards;
					nextFreeRewardAvailable = currentTime + timeBetweenFreeRewards;
					
					rewardCount++;
					Debug.Log("Licked for reward number " + rewardCount);
				}
				else if (currentTime > nextFreeRewardAvailable && freeRewardCount < totalFreeRewards)
                {
					cmdWrite = 2;
					lastRewardTime = currentTime;
					nextRewardAvailable = currentTime + timeBetweenRewards;
					nextFreeRewardAvailable = currentTime + timeBetweenFreeRewards;

					freeRewardCount++;
					rewardCount++;
					Debug.Log("Timed out for reward number " + rewardCount);
                }
            }

			int missedArdFrame = 0;
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
							sw_lick.Write(transform.position.z + "\t" + Time.realtimeSinceStartup + "\n");
							//sw_lick.Close();
						}
					}

					//rotaryTicks = int.Parse(lick_list[1]);
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
			}

			if (ArdUse == ArduinoUse.Uniduino)
			{
				_serialPort.DiscardInBuffer();
				_serialPort.DiscardOutBuffer();
			}

			//delta_T = Time.deltaTime;

			if (saveData == true)
			{
				try
				{
					int cueOrderPrint = 0;
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

			if (rewardCount >= numRewardsTotal)
            {
				triggerStopSession = true;
            }

			if (triggerStopSession)
			{
				Debug.Log("Stopping session at " + Time.realtimeSinceStartup + "\n");
				Debug.Log("Stopping 1 " + Time.realtimeSinceStartup);
				sw_startstop.Write("StopSignalPlanned" + "\t" + Time.realtimeSinceStartup + "\n");
				UnityEditor.EditorApplication.isPlaying = false;
			}
		}
	}

	void ConfigurePins()
	{
		if (ArdUse == ArduinoUse.Uniduino)
		{
			Debug.Log("Pins configured (player controller)");
		}
		else
		{
			Debug.Log("Using simulated running");
		}
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
			sw_par.Close();


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

			}

		}
	}
}
