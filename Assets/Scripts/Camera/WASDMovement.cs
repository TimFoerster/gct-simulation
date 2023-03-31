using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WASDMovement : MonoBehaviour
{
    float speed = 5;

    void LateUpdate()
    {
        if (Input.GetKey(KeyCode.A))
        {
            transform.position += Vector3.left * speed;
        } else if (Input.GetKey(KeyCode.W))
        {
            transform.position += Vector3.up *  speed;
        }
        else if (Input.GetKey(KeyCode.D))
        {
            transform.position += Vector3.right * speed;
        }
        else if (Input.GetKey(KeyCode.S))
        {
            transform.position += Vector3.down * speed;
        }
       
    }
}
