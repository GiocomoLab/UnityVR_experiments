using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FadeRewardZone : MonoBehaviour
{
    public PlayerController_ShiftingRewards playerController;
    public GameObject rewardZone;

    public float startTransparency = 1.0f;
    public float endTransparency = 0.0f;
    public bool resetRewardTransparency = false;
    public float transparencyIncreaseOnReset = 0.1f; // Can call this function to manually reset transparency
    private float resetTransparency;
    public float currentTransparency;

    public int trialsIncreaseTransparencyAfter = 5;
    public float increaseTransparencyBy = 0.1f;
    
    private int numTraversals;
    private int lastTransparencyChangeTrial = 0;
    private Vector3 lastPosition;

    Renderer rwzoneRenderer;
    // Start is called before the first frame update
    void Start()
    {
        resetTransparency = startTransparency;

        rwzoneRenderer = rewardZone.GetComponent<Renderer>();

        Material bl = rwzoneRenderer.material;
        Color color = bl.color;
        currentTransparency = color.a;
        Debug.Log("currentTransparency is " + currentTransparency);
        if (startTransparency > 1.0f)
        {
            startTransparency = 1.0f;
        }
        currentTransparency = startTransparency;
        color.a = currentTransparency;
        bl.color = color;
        rwzoneRenderer.material= bl;
        Debug.Log("Set reward zone transparency to " + currentTransparency);

        bl = rwzoneRenderer.material;
        color = bl.color;
        currentTransparency = color.a;
        Debug.Log("currentTransparency is " + currentTransparency);

        //Color color = rewardZone.color;
    }

    // Update is called once per frame
    void Update()
    {
        numTraversals = playerController.numTraversals;
        // Each traversal, if it's been enough trials, update transparency of reward zone
        if ((numTraversals - lastTransparencyChangeTrial) >= trialsIncreaseTransparencyAfter)
        {
            
            Material bl = rwzoneRenderer.material;
            Color color = bl.color;
            currentTransparency = color.a;
            currentTransparency = currentTransparency - increaseTransparencyBy;
            if (currentTransparency < endTransparency)
            {
                currentTransparency = endTransparency;
            }
            if (currentTransparency < 0f)
            {
                currentTransparency = 0f;
            }
            

            color.a = currentTransparency;
            rwzoneRenderer.material.color = color;
            Debug.Log("Set reward zone transparency to " + currentTransparency);

            lastTransparencyChangeTrial = numTraversals;
        }
        
        lastPosition = rewardZone.transform.position;
        bool lastPos = lastPosition[1] == 0.5f;
        rewardZone.transform.position = new Vector3(lastPosition[0], 0.5f, lastPosition[2]);
        if (currentTransparency <= 0f)
        {
            rewardZone.transform.position = new Vector3(lastPosition[0], -55.5f, lastPosition[2]);
            if (lastPos == true)
            {
                Debug.Log("moving reward zone down");
            }

        }
        
    }

    public void ResetTransparency()
    {
        if (resetRewardTransparency == true)
        {
            Material bl = rwzoneRenderer.material;
            Color color = bl.color;
            currentTransparency = resetTransparency;
            color.a = currentTransparency;
            rwzoneRenderer.material.color = color;

            resetTransparency = resetTransparency - transparencyIncreaseOnReset;
        }
    }
}
