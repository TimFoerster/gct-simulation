using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[RequireComponent(typeof(GenericPersonAi))]
public class ParkPerson : MonoBehaviour, IPerson
{
    public GenericPersonAi person { get; set; }
    [SerializeField] PersonMovement personMovement;

    public float Speed { set => personMovement.speed = value; }

    public bool OnEnterExit()
    {
        return true;
    }

    enum State
    {
        Init,
        GoingToDestination,
        Relaxing,
        Leaving
    }


    [ReadOnly, SerializeField]
    State state;

    public Vector3 destination;
    public ParkWaypoint[] waypoints;
    public int waypointIndex;

    float waitTill;

    [SerializeField]
    SimulationTime time;

    void Awake()
    {
        person = GetComponent<GenericPersonAi>();
        person.PersonInstance = this;
        personMovement = GetComponent<PersonMovement>();
        time = FindObjectOfType<SimulationTime>();

    }

    void FixedUpdate()
    {
        switch (state)
        {
            case State.Init:
                GoToDestination();
                return;

            case State.GoingToDestination:
                if (!personMovement.destinationReached) return;
                OnDestinationReached();
                return;
            case State.Relaxing:
                if (waitTill > time.time) return;
                NextWaypointOrLeave();
                return;

            case State.Leaving:
                return;

        }
    }

    void GoToDestination()
    {
        destination = waypoints[waypointIndex].destination;
        person.MoveTo(destination, 0);
        state = State.GoingToDestination;
    }

    void OnDestinationReached()
    {
        waitTill = time.time + waypoints[waypointIndex].waitFor;
        state = State.Relaxing;
    }

    void NextWaypointOrLeave()
    {
        if (person.leaveTime <= time.time || waypoints.Length - 1 == waypointIndex)
        {
            person.GoToExit();
            state = State.Leaving;
            return;
        }

        waypointIndex++;
        GoToDestination();

    }
}
