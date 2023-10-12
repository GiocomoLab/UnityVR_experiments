using UnityEngine;
using System.Collections;
//using Uniduino;
using System;
using System.IO;

public class SavePositionData2 : MonoBehaviour
{

	private SessionParams paramsScript;
	private string mouse;
	private string session;
	private bool saveData;
	private string positionFile;
	private string serverPositionFile;
	private string localDirectory;
	private string serverDirectory;
	private StreamWriter sw_pos; // stream writer for position file

	//public Arduino arduino;
	private int syncPin = 8;  // sync pulse to NIDAQ
	private float lastSyncT = 0;
	private int triggerPin = 11;  // send trigger to faceCam
	private int triggerValue = 0;
	private float lastTriggerT = 0;
	private int syncPinValue = 0;
	public bool recordingStarted = false;
	private int triggerFlag = 0;

	void Start()
	{
		// set VR to make a frame for every frame of the monitor
		QualitySettings.vSyncCount = 1;

		// initialize arduino
		//arduino = Arduino.global;
		//arduino.Setup(ConfigurePins);
		//arduino.digitalWrite (syncPin, Arduino.LOW);
		//arduino.digitalWrite (triggerPin, Arduino.LOW);

		GameObject player = GameObject.Find("Player");
		paramsScript = player.GetComponent<SessionParams>();
		mouse = paramsScript.mouse;
		session = paramsScript.session;
		saveData = paramsScript.saveData;
		localDirectory = paramsScript.localDirectory;
		serverDirectory = paramsScript.serverDirectory;
		positionFile = localDirectory + "\\" + mouse + "\\" + session + "_position.txt";
		serverPositionFile = serverDirectory + "\\" + mouse + "\\VR\\" + session + "_position.txt";
		if (saveData)
		{
			sw_pos = new StreamWriter(positionFile, true);
		}
	}

	void ConfigurePins()
	{
		//arduino.pinMode (syncPin, PinMode.OUTPUT);
		//arduino.pinMode (triggerPin, PinMode.OUTPUT);
		Debug.Log("Pins configured (synchronize computers)");
	}

	void Update()
	{
		if (!recordingStarted)
		{
			// wait for keypress to start session
			if (Input.GetKeyDown("return"))
			{
				recordingStarted = true;
				Debug.Log("Session started");
			}
		}

		// write position data to file every frame
		// flip syncPin every frame
		if (recordingStarted & saveData)
		{
			syncPinValue = (syncPinValue + 1) % 2;
			//arduino.digitalWrite (syncPin, syncPinValue);
			sw_pos.Write(transform.position.z + "\t" + Time.realtimeSinceStartup + "\n");

			//			// trigger faceCam to start capturing video (just once)
			//			if (Time.realtimeSinceStartup - lastTriggerT > 0.04)
			//			{
			//                lastTriggerT = Time.realtimeSinceStartup;
			//				triggerValue = 1;
			//				arduino.digitalWrite(triggerPin, triggerValue);  // trigger faceCam
			//				triggerFlag = 1;
			//            }
			//            else {
			//				triggerValue = 0;
			//				arduino.digitalWrite(triggerPin, triggerValue);
			//			}
		}

		// write position data to file at 50Hz
		//		if (recordingStarted & saveData)
		//		{
		//			if (Time.realtimeSinceStartup - lastSyncT >= 0.02){
		//				lastSyncT = Time.realtimeSinceStartup;
		//				syncPinValue = 1;
		//				arduino.digitalWrite (syncPin, syncPinValue);
		//				sw_pos.Write (transform.position.z +  "\t" + Time.realtimeSinceStartup + "\n");
		//			}
		//			else {
		//				syncPinValue = 0;
		//				arduino.digitalWrite (syncPin, syncPinValue);
		//			}
		//
		//			// trigger faceCam to start capturing video (just once)
		//			if (Time.realtimeSinceStartup - lastTriggerT > 0.04)
		//			{
		//				lastTriggerT = Time.realtimeSinceStartup;
		//				triggerValue = 1;
		//				arduino.digitalWrite(triggerPin, triggerValue);  // trigger faceCam
		//				triggerFlag = 1;
		//			}
		//			else {
		//				triggerValue = 0;
		//				arduino.digitalWrite(triggerPin, triggerValue);
		//			}
		//		}
	}

	void OnApplicationQuit()
	{
		//arduino.digitalWrite (syncPin, Arduino.LOW);
		//arduino.digitalWrite(triggerPin, Arduino.LOW);
		if (saveData)
		{
			sw_pos.Close();
			File.Copy(positionFile, serverPositionFile);
		}
	}

}
