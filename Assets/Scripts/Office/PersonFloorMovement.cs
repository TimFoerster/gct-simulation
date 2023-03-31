using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Events;

[RequireComponent(typeof(GenericPersonAi))]
public class PersonFloorMovement : MonoBehaviour
{
    enum State
    {
        Idle,
        GoingToLift,
        WaitingForLift,
        LiftAvailable,
        GoingToLiftPosition,
        InLift,
        Reached
    }

    public Lift lift;

    [SerializeField]
    Transform liftPos;

    public Floor floor;
    public Floor targetFloor;

    GenericPersonAi person;
    PersonMovement personMovement;

    [SerializeField, ReadOnly]
    State state;

    UnityAction onFloorReached;

    BLEReceiver bleReceiver;

    RandomNumberGenerator rng;

    private void Awake()
    {
        personMovement = GetComponent<PersonMovement>();
        person = GetComponent<GenericPersonAi>();
        floor = GetComponentInParent<Floor>();
        bleReceiver = GetComponentInParent<BLEReceiver>();
        rng = GetComponent<RandomNumberGenerator>();
    }

    private void FixedUpdate()
    {
        switch (state)
        {
            case State.GoingToLift:
                if (!personMovement.destinationReached) return;
                OnLiftAreaReached();
                break;

            case State.GoingToLiftPosition:
                if (!personMovement.destinationReached) return;
                OnLiftPositionReached();
                break;

            case State.LiftAvailable:
                GoInLift();
                break;
        }
    }

    private void OnLiftPositionReached()
    {
        state = State.InLift;
        floor.RemoveReceiver(bleReceiver);
        floor = null;
        personMovement.MakeStatic();
        transform.localPosition = new Vector3(transform.localPosition.x, transform.localPosition.y, 0);
    }

    internal void GoToFloor(Floor targetFloor, UnityAction onFloorReached)
    {
        this.targetFloor = targetFloor;
        this.onFloorReached = onFloorReached;
        GoToLift();
    }

    internal void OnLiftAvailable(Lift lift)
    {
        var obj = lift.ExpectPerson(this, targetFloor);
        if (obj == null)
        {
            OnLiftAreaReached();
            return;
        }

        state = State.LiftAvailable;
        liftPos = obj;
        this.lift = lift;
    }

    internal void GoInLift()
    {
        person.MoveTo(liftPos.transform.position, floor.number);
        state = State.GoingToLiftPosition;
    }

    void GoToLift()
    {
        state = State.GoingToLift;
        person.MoveTo(Utils.RandomPositionInBounds(floor.liftArea.bounds, rng), floor.number);
    }

    void OnLiftAreaReached()
    {
        state = State.WaitingForLift;

        var lift = floor.RequestingLift(this, targetFloor);

        if (lift == null)
            return;

        OnLiftAvailable(lift);
    }

    internal void OnFloorArrive(Floor currentFloor, Lift lift)
    {
        if (currentFloor != targetFloor) return;

        person.OnFloorEnter(currentFloor.number);
        state = State.Reached;
        floor = currentFloor;
        floor.NewReceiver(bleReceiver);
        lift.PersonLeaving(this, liftPos);
        liftPos = null;
        this.lift = null;
        onFloorReached.Invoke();
    }

    internal bool readyToMove => state == State.InLift;

    void OnDisable()
    {
        if (floor)
        {
            floor.RemoveReceiver(bleReceiver);
        }
    }

    void OnEnable()
    {
        if (floor)
        {
            floor.NewReceiver(bleReceiver);
        }
    }
}
