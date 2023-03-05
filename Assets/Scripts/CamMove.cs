using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamMove : MonoBehaviour
{

    // Move camera based on input
    void Update()
    {
        if (Input.GetKey(KeyCode.W))
        {
            transform.position += transform.forward * Time.deltaTime * 10;
        }
        if (Input.GetKey(KeyCode.S))
        {
            transform.position -= transform.forward * Time.deltaTime * 10;
        }
        if (Input.GetKey(KeyCode.A))
        {
            transform.position -= transform.right * Time.deltaTime * 10;
        }
        if (Input.GetKey(KeyCode.D))
        {
            transform.position += transform.right * Time.deltaTime * 10;
        }
        if (Input.GetKey(KeyCode.Q))
        {
            transform.position -= transform.up * Time.deltaTime * 10;
        }
        if (Input.GetKey(KeyCode.E))
        {
            transform.position += transform.up * Time.deltaTime * 10;
        }
    }
}
