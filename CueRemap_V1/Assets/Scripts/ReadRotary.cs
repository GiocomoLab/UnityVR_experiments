using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.IO.Ports;
using System.Threading;

public class ReadRotary : MonoBehaviour {

	// for reading Arduino from serial port
	public string port = "/dev/tty.usbmodem1441";
	private SerialPort _serialPort;
	private int delay;
	private int pulses;
	private float speed;
	private float originalSpeed;
	private Vector3 lastPosition;

	// for gain manipulations
	private float gainValue;

	// other scripts
	private SessionParams paramsScript;
	//private PlayerController playerScript;
	private PlayerController3 playerScript;
	//private SavePositionData saveScript;
	private SavePositionData2 saveScript;
	private bool recordingStarted_local = false;

	private PlayerController3 controllerScript;
	private bool simulateRunning;
	private float simRunSpeed;
	private bool allowMovement;
	void Start()
	{
		Debug.Log("ReadRotary start");
		// connect to playerController script
		GameObject player = GameObject.Find("Player");
		//playerScript = player.GetComponent<PlayerController> ();
		paramsScript = player.GetComponent<SessionParams>();
		//saveScript = player.GetComponent<SavePositionData>();
		saveScript = player.GetComponent<SavePositionData2>();

		//GameObject player = GameObject.Find("Player");
		playerScript = player.GetComponent<PlayerController3>();
		simulateRunning = playerScript.simulatedRunning;
		simRunSpeed = playerScript.simRunSpeed;
		Debug.Log("ReadRotary Simulated running is: " + simulateRunning);

		// connect to Arduino uno serial port
		if (simulateRunning == false)
		{
			connect(port, 57600, true, 3);
			Debug.Log("Connected to rotary encoder serial port");
		}
		else
        {
			Debug.Log("Simulating running speed instead of connecting to rotary encoder");
        }
		// set speed and last position
		speed = 0;
		lastPosition = transform.position;

		
	}

	void Update() 
	{
		allowMovement = playerScript.allowMovement;
		
		if (recordingStarted_local == true)
		{
			//Debug.Log("laps, total: " + playerScript.numTraversals + playerScript.numTrialsTotal);
		}

		//Debug.Log("ssRecStart: " + saveScript.recordingStarted);
		if (saveScript.recordingStarted & !recordingStarted_local) 
		{
			speed = 0.0447f; // makes 1 VR unit = 1 cm for 18 inch circumference cylinder
			originalSpeed = speed;
			recordingStarted_local = true;
		}
		//if (playerScript.numTraversals >= paramsScript.numTrialsTotal) {
		if (playerScript.numTraversals >= playerScript.numTrialsTotal)
		{
			speed = 0;
		}

		// read quadrature encoder and move player accordingly
		if (simulateRunning == false)
		{
			_serialPort.Write("\n");
			pulses = int.Parse(_serialPort.ReadLine());
		}
        else
        {
			float posUpdateZ = Time.deltaTime * simRunSpeed;
			float pulsesFloat = posUpdateZ / speed;
			pulses = (int) pulsesFloat;
			if (recordingStarted_local == true)
			{
				//Debug.Log("speed, simRunSpeed, posUpdateZ, pulsesFloat, simulated pulses " + 
				//	" " + speed + " " + simRunSpeed + " " + posUpdateZ + " " + pulsesFloat + " " + pulses);
			}
        }

		if (pulses == 0) {
			transform.position = lastPosition;
		} else {
			float delta_z = pulses * speed;
			float current_z = transform.position.z;
			if (allowMovement == true)
			{
				if (current_z + delta_z > -5.0f)
				{
					if (current_z + delta_z > 400)
					{
						Debug.Log("This step will cross the line");
					}
					transform.position = new Vector3(0.0f, 0.5f, current_z + delta_z);
				}

				
					/*
					if (transform.position.z < -5.0f)
					{
						Debug.Log("caught the mouse slipping backwards");
						transform.position.z = 0f; // For some reason getting an error here?
					}
					*/
				}
		}
		lastPosition = transform.position;

		// change speed by gain value
		speed = originalSpeed * playerScript.gain;

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
		Close ();		
	}
	
	void OnDestroy()
	{				
		Disconnect();
	}

}
