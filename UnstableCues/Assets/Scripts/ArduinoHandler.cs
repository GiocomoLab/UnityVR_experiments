using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Threading;

public class ArduinoHandler : MonoBehaviour
{
    public string thisPort = "COM6";
    private SerialPort _serialPort;
    private int delay;

    private string lick_raw;
    [HideInInspector] public int lickPinValue;
    [HideInInspector] public int rotaryTicks;
    [HideInInspector] public int syncPinState;
    [HideInInspector] public int startStopPinState;
    [HideInInspector] public int missedArdFrame;

    void Awake()
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

        _serialPort.DiscardInBuffer();
        _serialPort.DiscardOutBuffer();
    }
    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Handshaking Arduino connection...");
        bool connectedArd = false;
        int triesA = 0;
        float TimeStartedConn = Time.realtimeSinceStartup;
        while (connectedArd == false)
        {
            triesA++;
            int cmdWrite = 10;
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

    // Update is called once per frame
    void Update()
    {
        
    }

    public void sendStartSignal()
    {
        int cmdWrite = 4;
        _serialPort.Write(cmdWrite.ToString() + ',');
        _serialPort.DiscardInBuffer();
        _serialPort.DiscardOutBuffer();
    }

    public void sendCmdReadReply(int cmdWrite)
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

            rotaryTicks = int.Parse(lick_list[1]);
            syncPinState = int.Parse(lick_list[2]);
            startStopPinState = int.Parse(lick_list[3]);
        }
        catch (TimeoutException)
        {
            Debug.Log("lickport/encoder timeout on frame " + Time.frameCount);
        }
        catch
        {
            Debug.Log("cmd " + cmdWrite + " reply " + lick_raw + " frame " + Time.frameCount);
        }

        _serialPort.DiscardInBuffer();
        _serialPort.DiscardOutBuffer();
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
}
