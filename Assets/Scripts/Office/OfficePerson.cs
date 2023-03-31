using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[RequireComponent(typeof(GenericPersonAi))]
[RequireComponent(typeof(PersonFloorMovement))]
public class OfficePerson : MonoBehaviour, IPerson
{
    enum State {
        Init,
        ChangingLevel,
        GoingToWorkplace,
        Working,
        Break,
        Leaving,
    }

    enum WorkState
    {
        Coming,
        Working,
        Break,
        Done
    }


    public GenericPersonAi person { get; set; }

    [ReadOnly, SerializeField]
    State state;

    [ReadOnly, SerializeField]
    WorkState workState;

    PersonFloorMovement personFloorMovement;

    [SerializeField]
    PersonMovement personMovement;

    public Workspace workspace;

    Floor floor => personFloorMovement.floor;
    Lift lift => personFloorMovement.lift;

    [SerializeField]
    SimulationTime time;


    private void Awake()
    {
        person = GetComponent<GenericPersonAi>();
        person.PersonInstance = this;
        personFloorMovement = GetComponent<PersonFloorMovement>();
        time = FindObjectOfType<SimulationTime>();
    }
    
    // Start is called before the first frame update
    void Start()
    {
        if (state == State.Init && workState == WorkState.Done)
        {
            state = State.ChangingLevel;
            GoToFloor(
                personFloorMovement.floor.building.ExitFloor,
                person.GoToExit);
        }

    }


    void FixedUpdate()
    {
        // Do nothing, wenn a need is required
        if (person.HasNeed) return; 

        switch (state)
        {
            case State.Init:
                if (floor != workspace.floor)
                {
                    GoToFloor(workspace.floor, GoToWorkplace);
                    return;
                }

                GoToWorkplace();
                return;

            case State.GoingToWorkplace:

                if (floor != workspace.floor) return;
                if (!personMovement.destinationReached) return;
                OnWorkspaceReached();
                return;

            case State.Working:
                var need = person.CheckForNeeds();
                if (need != null)
                {
                    // State after need is fullfield
                    state = State.Init;
                    // fullfiell need
                    need.FullfillNeed();
                    return;
                }

                if (person.leaveTime <= time.time) 
                    OnWorkDone();
                break;
        }
    }

    public BLEReceiver[] ReceiverSearchGroup()
    {
        if (floor)
            return floor.Receivers.ToArray();
        if (lift)
            return lift.GetComponentsInChildren<BLEReceiver>();

        return FindObjectsOfType<BLEReceiver>();
    }

    void GoToWorkplace()
    {
        person.MoveTo(workspace.transform);
        state = State.GoingToWorkplace;
    }

    void OnWorkspaceReached()
    {
        state = State.Working;
        workState = WorkState.Working;
        person.MakeMovementStatic();
    }

    void OnWorkDone()
    {
        person.MakeMovementDynamic();
        workState = WorkState.Done;
        GoToFloor(floor.building.ExitFloor, GoToExit);
    }

    void GoToFloor(Floor targetFloor, UnityAction action)
    {
        state = State.ChangingLevel;

        if (floor == targetFloor)
        {
            action.Invoke();
            return;
        } 
        personFloorMovement.GoToFloor(targetFloor, action);
    }

    void GoToExit()
    {
        state = State.Leaving;
        person.GoToExit();
    }

    internal void OnLeave()
    {
        state = State.Leaving;
    }

    public bool OnEnterExit()
    {
        return true;
    }

    public float Speed { set => personMovement.speed = value; }
}
