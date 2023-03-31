using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Lift))]
public class LiftMovement : MonoBehaviour
{

    Lift lift;

    // 2.5 m/s
    [SerializeField]
    float TopSpeed = 2.5f;

    // 0.6 m/s^2
    [SerializeField]
    float acceleration = 0.6f;


    float speed = 0f;
    float movement = 0f;

    Vector3 destination;

    public float remainingDistance => (transform.position - destination).magnitude;
    public float remainingSqrDistance => (transform.position - destination).sqrMagnitude;


    void Awake()
    {
        lift = GetComponent<Lift>();
    }

    public void GoTo(Vector3 target)
    {
        destination = target;
    }

    void FixedUpdate()
    {
        if (lift.state != LiftState.Moving) return;

        var rd = remainingDistance;

        if (rd < 0.00001f)
        {
            movement = 0f;
            lift.OnArrive();
            return;
        }

        if (rd < speed * speed )
        {
            movement = Mathf.Max(movement - acceleration * Time.fixedDeltaTime, .005f);
        }
        else
        {
            movement = Mathf.Min(movement + acceleration * Time.fixedDeltaTime, 1);
        }

        speed = TopSpeed * movement;
        transform.position = Vector3.MoveTowards(
            transform.position, 
            destination, 
            speed * Time.fixedDeltaTime);
    }
}
