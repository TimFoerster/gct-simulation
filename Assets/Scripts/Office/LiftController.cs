using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class FloorLiftQueue 
{
    public Floor floor;
    public LiftDirection direction;

    public FloorLiftQueue(Floor floor, LiftDirection direction)
    {
        this.floor = floor;
        this.direction = direction;
    }
}

public class LiftController : MonoBehaviour
{

    Lift[] lifts;
    List<Lift> idleLifts;

    [SerializeField]
    List<FloorLiftQueue> floorQueue;

    // Use this for initialization
    void Awake()
    {
        lifts = GetComponentsInChildren<Lift>();
        idleLifts = new List<Lift>();
    }

    public void CallLift(Floor floor, LiftDirection direction = LiftDirection.None)
    {
        if (idleLifts.Count == 0)
        {
            floorQueue.Add(new FloorLiftQueue(floor, direction));
            return;
        }

        sendLiftToFloor(idleLifts.First(), floor, direction);
    }

    public void Idle(Lift lift)
    {
        if (floorQueue.Count == 0)
        {
            idleLifts.Add(lift);
            return;
        }

        var floor = floorQueue.First();
        if (floor == null) return;

        sendLiftToFloor(lift, floor.floor, floor.direction);
    }

    void sendLiftToFloor(Lift lift, Floor floor, LiftDirection direction)
    {
        idleLifts.Remove(lift);
        floorQueue.RemoveAll(q => q.floor == floor && q.direction == direction);
        lift.Direction = direction;
        lift.ControllerSendingLiftToFloor(floor);
    }

    internal void Busy(Lift lift)
    {
        idleLifts.Remove(lift);
    }
}
