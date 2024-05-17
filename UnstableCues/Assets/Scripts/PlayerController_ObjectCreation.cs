using UnityEngine;
using UnityEditor;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Ports;
using System.Threading;

public class PlayerController_ObjectCreation : MonoBehaviour
{
    public string thisPort = "COM6";
    public bool useArduino = false;
    public float simulatedSpeed = 100.0f;

    public float trackLen = 350.0f;

    public int numTraversals = 0;
    public int totalLaps = 10;

    public ObjectMover objectMover;

    private float delta_z;

    void Wake()
    {
        Debug.Log("Began wake");
        // initialize arduino
       

        Debug.Log("End of wake");
    }

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Start of start");

       

        //rwMarkerPos = new Vector3(trackOffset_x, -18f, rewardPosition);
        if (useArduino == true)
        {
        }
    }

    // Update is called once per frame
    void Update()
    {
        delta_z = simulatedSpeed * Time.deltaTime;
        if (useArduino == true)
        {
            
        }

        Vector3 lastPosition = transform.position;
        lastPosition[2] = lastPosition[2] + delta_z;

        if (transform.position.z > trackLen)
        {
            lastPosition[2] = 0.0f;
            numTraversals++;

            objectMover.RandomizeObject();

            Debug.Log("Lap number " + numTraversals);

        }

        if (numTraversals >= totalLaps)
        {

        }
        transform.position = lastPosition;

        float objMoverPos = objectMover.posTen;
        if (objMoverPos > 200)
        {
            Debug.Log("!!");
        }

    }

}
