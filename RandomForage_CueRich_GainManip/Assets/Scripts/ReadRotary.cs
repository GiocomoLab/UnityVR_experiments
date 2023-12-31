using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.IO.Ports;
using System.Threading;

public class ReadRotary : MonoBehaviour {

	// for reading Arduino from serial port
	//public string port = "/dev/tty.usbmodem1441";
	public string port = "COM5";
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
	private PlayerController2 playerScript;
	private SavePositionData2 saveScript;
	private bool recordingStarted_local = false;

	void Start()
	{
		// connect to Arduino uno serial port
		try
		{
		connect (port, 57600, true, 3);
		Debug.Log ("Connected to rotary encoder serial port");
		}
		catch
		{
			Debug.Log ("Failed to connect to rotary encoder serial port");
		}
		// set speed and last position
		speed = 0;
		lastPosition = transform.position;

		// connect to playerController script
		GameObject player = GameObject.Find ("Player");
		playerScript = player.GetComponent<PlayerController2> ();
		paramsScript = player.GetComponent<SessionParams> ();
		saveScript = player.GetComponent<SavePositionData2> ();

	}

	void Update() 
	{
		if (saveScript.recordingStarted & !recordingStarted_local) 
		{
			speed = 0.0447f; // makes 1 VR unit = 1 cm for 18 inch circumference cylinder
			originalSpeed = speed;
			recordingStarted_local = true;
		}
		if (playerScript.numTraversals >= paramsScript.numTrialsTotal) {
			speed = 0;
		}

		// read quadrature encoder and move player accordingly
		_serialPort.Write("\n");
		pulses = int.Parse (_serialPort.ReadLine ());

		if (pulses == 0) {
			transform.position = lastPosition;
		} else {
			float delta_z = pulses * speed;
			float current_z = transform.position.z;			
			transform.position = new Vector3 (0.0f, 0.5f, current_z + delta_z);
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
