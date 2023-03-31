using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(LiftMovement))]
public class Lift : MonoBehaviour
{
    LiftController liftController;

    [SerializeField]
    Floor targetFloor;

    Floor currentFloor;

    LiftMovement movement;

    public LiftState state;
    LiftDirection direction;
    public LiftDirection Direction { get => direction; set => direction = value; }

    [SerializeField]
    List<PersonFloorMovement> persons;

    [SerializeField]
    Transform[] standingPositions;

    [SerializeField]
    GameObject standingPositionsObject;

    [SerializeField]
    List<Transform> freePositions;

    [SerializeField]
    List<Floor> stops;

    [SerializeField]
    float offset = 0;

    [SerializeField]
    Transform Persons;

    float minWaitingTime = 5;

    float waitingTime = 0;

    void Start()
    {
        movement = GetComponent<LiftMovement>();
        standingPositions = standingPositionsObject.GetComponentsInChildren<Transform>()
            .Where(t => t != standingPositionsObject.transform)
            .ToArray();
        freePositions = standingPositions.ToList();
        liftController = GetComponentInParent<LiftController>();
        GoToFloor(targetFloor);
    }

    void FixedUpdate()
    {
        switch (state)
        {
            case LiftState.Arrived:
                if (Time.time < waitingTime) return;

                // If noone is waiting and no direction set, notify controller
                if (stops.Count == 0 && persons.Count == 0)
                {
                    state = LiftState.Idle;
                    liftController.Idle(this);
                    return;
                }

                state = LiftState.WantsToLeave;
                currentFloor.OnLiftLeave(this);
                return;

            case LiftState.WantsToLeave:

                if (Time.time < waitingTime) return;

                waitingTime = Time.time + minWaitingTime;

                // wait till every person is ready
                if (persons.Find(p => !p.readyToMove) != null) return;

                nextStop();
                return;
        }
    }


    
    void nextStop()
    {
        int number;

        if (direction == LiftDirection.Down)
            number = stops.Max(s => s.number);
        else
            number = stops.Min(s => s.number);

        GoToFloor(stops.First(s => s.number == number));
    }

    public void OnArrive()
    {
        currentFloor = targetFloor;
        stops.Remove(currentFloor);
        targetFloor = null;
        state = LiftState.Arrived;
        waitingTime = Time.time + minWaitingTime;
        direction = stops.Count == 0 ? LiftDirection.None : direction;
        persons.ToList().ForEach(p => p.OnFloorArrive(currentFloor, this));
        currentFloor.OnLiftArrive(this);
    }

    public bool IsGoingDown => 
        direction == LiftDirection.Down;

    public bool IsGoingUp =>
        direction == LiftDirection.Up;

    public bool IsFull => freePositions.Count == 0;
    public bool IsIdle => direction == LiftDirection.None;

    public void ControllerSendingLiftToFloor(Floor floor)
    {
        state = LiftState.WantsToLeave;
        currentFloor.OnLiftLeave(this);
        stops.Add(floor);
    }
    public void GoToFloor(Floor floor)
    {
        targetFloor = floor;
        direction = floor.number < (currentFloor ? currentFloor.number : int.MinValue) ?
            LiftDirection.Down : LiftDirection.Up;

        var pos = transform.position;
        state = LiftState.Moving;
        movement.GoTo(new Vector3(pos.x, floor.liftArea.transform.position.y + offset, floor.transform.position.z));
    }

    internal Transform ExpectPerson(PersonFloorMovement person, Floor floor)
    {
        if (freePositions.Count == 0) return null;

        if (state == LiftState.Idle)
        {
            liftController.Busy(this);
        }

        var pos = freePositions.First();
        freePositions.Remove(pos);
        persons.Add(person);
        person.transform.parent = Persons;

        if (direction == LiftDirection.None)
            direction = floor.number < currentFloor.number ?
                LiftDirection.Down :
                LiftDirection.Up;

        state = LiftState.Arrived;
        waitingTime = Time.time + minWaitingTime;
        stops.Add(floor);

        return pos;
    }

    internal void PersonLeaving(PersonFloorMovement person, Transform standingPosiiton)
    {
        freePositions.Add(standingPosiiton);
        persons.Remove(person);
        person.transform.parent = currentFloor.Persons;
    }
}
