using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectMover : MonoBehaviour
{
    public GameObject towerA;
    public GameObject towerB;

    public GameObject[] objectsA;

    public float posA = 0.0f;
    public float posB = 40.0f;

    private Vector3 objApos;
    private Vector3 objBpos;

    private Vector3 towerPositionA;
    private Vector3 towerPositionB;

    [HideInInspector]
    public float posTen;
    // Start is called before the first frame update
    void Start()
    {
        towerPositionA = towerA.transform.position;
        towerPositionA[2] = posA;
        towerA.transform.position = towerPositionA;

        towerPositionB = towerB.transform.position;
        towerPositionB[2] = posB;
        towerB.transform.position = towerPositionB;
    }

    // Update is called once per frame
    void Update()
    {
        posTen = transform.position.z * 10.0f;
        if (transform.position.z > 250.0f && transform.position.z < 350.0f)
        {
            //RandomizeObject();
        }
    }

    public void RandomizeObject()
    {
        Debug.Log("Randomized objects");
        float randomNum = 15.0f;

        towerPositionA[2] = towerPositionA[2] + randomNum;
        towerA.transform.position = towerPositionA;

        towerPositionB[2] = towerPositionB[2] - randomNum;
        towerB.transform.position = towerPositionB;
    }
}
