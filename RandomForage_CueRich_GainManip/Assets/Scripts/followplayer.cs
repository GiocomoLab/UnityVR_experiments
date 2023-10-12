using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class followplayer : MonoBehaviour
{

    public GameObject trackTarget;
    public bool followPlayer;
    private Vector3 startOffsets;
    private Vector3 targetInitialPos;
    private Vector3 targetCurrentPos;
    private Vector3 followOffsets;
    private bool haveInitialPos;

    // Start is called before the first frame update
    void Start()
    {
        startOffsets = transform.position;    
        if (followPlayer == false)
        {
            Vector3 hidePos = new Vector3(-500.0f, -500.0f, -500.0f);
            trackTarget.transform.position = hidePos;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (followPlayer == true)
        {
            if (haveInitialPos == false)
            {
                targetInitialPos = trackTarget.transform.position;
                followOffsets = startOffsets - targetInitialPos;
                haveInitialPos = true;
            }

            targetCurrentPos = trackTarget.transform.position;
            transform.position = targetCurrentPos + followOffsets;
        }
    }
}
