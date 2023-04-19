using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;

public class Screenshot : MonoBehaviour
{

    /**
     * FOR PERFECT SCREENSHOTS:
     * CAMERA:
     * - POSITION: 65, 53, -203.2983
     * - SIZE: 40 (Orthographic camera)
     *
     * OUTPUT:
     * - POSITION: 0, 0, 0
     * - TILESET SIZE: 142 x 80
     * - GRID(TILE) SIZE: 1
     */
    private int imageIter = 1;

    private float flashTimer = 0f;

    private GameObject light;

    void Start()
    {
        light = GameObject.Find("Directional Light");
    }

    private void Update()
    {
        if (Input.GetKeyUp(KeyCode.Space))
        {
            flashTimer = 0.5f;
            TakeScreenshot();
            Debug.Log("Screen shot " + imageIter + " taken!");
            imageIter++; // Increments every time a screenshot is captured so the older screenshots will not be overwritten.
        }

        if (flashTimer > 0f)
        {
            light.GetComponent<Light>().intensity = 5f;
        }
        else
        {
            light.GetComponent<Light>().intensity = 1f;
        }

        flashTimer -= Time.deltaTime;
    }


    public void TakeScreenshot()
    {
        ScreenCapture.CaptureScreenshot( "IMAGES/output" + imageIter + ".png");
    }
}
