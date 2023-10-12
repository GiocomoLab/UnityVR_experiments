using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShroudTransparancy : MonoBehaviour
{


    [Range(0.0f, 1.0f)]
    public float transparency;

    public GameObject frontObj;
    public GameObject leftObj;
    public GameObject rightObj;
    public GameObject bottomObj;

    Renderer rendF;
    Renderer rendL;
    Renderer rendR;
    Renderer rendB;


    // Start is called before the first frame update
    void Start()
    {
        rendF = frontObj.GetComponent<Renderer>();
        rendL = leftObj.GetComponent<Renderer>();
        rendR = rightObj.GetComponent<Renderer>();
        rendB = bottomObj.GetComponent<Renderer>();
    }

    // Update is called once per frame
    void Update()
    {
        Material bl = rendF.material;
        Color color = bl.color;
        color.a = transparency;

        rendF.material.color = color;

        //bl = rendL.material;
        //color = bl.color;
        //color.a = transparency;

        rendL.material.color = color;

        //bl = rendR.material;
        //color = bl.color;
        //color.a = transparency;

        rendR.material.color = color;

        //bl = rendB.material;
        //color = bl.color;
        //color.a = transparency;

        rendB.material.color = color;
    }
}
