using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public class LiftQeueue
{

    public PersonFloorMovement person;
    public Floor targetFloor;

    public LiftQeueue(PersonFloorMovement person, Floor targetFloor)
    {
        this.person = person;
        this.targetFloor = targetFloor;
    }
}

public class Floor : MonoBehaviour, HasReceivers
{
    public int number;

    [SerializeField]
    Transform persons;

    public Transform Persons { get => persons; }

    [SerializeField]
    List<LiftQeueue> liftQueue;

    [SerializeField]
    List<Lift> lifts;

    public LiftArea liftArea;

    internal OfficeBuilding building;
    LiftController liftController;

    public Workspace[] workspaces;

    bool liftCalledDown = false;
    bool liftCalledUp = false;

    [SerializeField]
    List<BLEReceiver> bleReceivers;

    // Start is called before the first frame update
    void Awake()
    {
        building = GetComponentInParent<OfficeBuilding>();
        liftController = GetComponentInParent<LiftController>();
        liftArea = GetComponentInChildren<LiftArea>();
        workspaces = GetComponentsInChildren<Workspace>();

        foreach (var receiver in GetComponentsInChildren<BLEReceiver>())
        {
            bleReceivers.Add(receiver);
        }
    }

    public void OnLiftArrive(Lift lift)
    {
        if (lift.Direction != LiftDirection.Down && liftCalledUp)
            liftCalledUp = false;
        else if (lift.Direction != LiftDirection.Up && liftCalledDown)
            liftCalledDown = false;

        lifts.Add(lift);
        LiftQeueue[] persons = {};
        var direction = lift.Direction;

        if (direction == LiftDirection.None && liftQueue.Count > 0)
        {
            direction = liftQueue[0].targetFloor.number < number ? 
                LiftDirection.Down : LiftDirection.Up;
        }

        switch (direction)
        {
            case LiftDirection.Down:
                persons = liftQueue.Where(q => q.targetFloor.number < number).ToArray();
                break;
            case LiftDirection.Up:
                persons = liftQueue.Where(q => q.targetFloor.number > number).ToArray();
                break;
        }

        foreach (var q in persons)
        {
            if (!lift.IsFull)
            {
                q.person.OnLiftAvailable(lift);
                liftQueue.Remove(q);
            }
        }
    }

    internal void OnLiftLeave(Lift lift)
    {
        lifts.Remove(lift);
    }

    internal Lift RequestingLift(PersonFloorMovement person, Floor targetFloor)
    {

        var direction = targetFloor.number < number ? LiftDirection.Down : LiftDirection.Up;

        foreach (var lift in lifts)
        {
            if (lift.IsFull) continue;

            if (lift.IsIdle ||
                (direction == LiftDirection.Down && lift.IsGoingDown) ||
                (direction == LiftDirection.Up && lift.IsGoingUp))
            {
                return lift;
            }

        }

        liftQueue.Add(new LiftQeueue(person, targetFloor));

        // if not a lift has been called yet
        if ((direction == LiftDirection.Up && !liftCalledUp)) {
            liftController.CallLift(this, direction);
            liftCalledUp = true;
        }
        else if (direction == LiftDirection.Down && !liftCalledDown) {
            liftController.CallLift(this, direction);
            liftCalledDown = true;
        }

        return null;


    }

    public void NewReceiver(BLEReceiver obj)
    {
        bleReceivers.Add(obj);
    }

    public void RemoveReceiver(BLEReceiver obj)
    {
        bleReceivers.Remove(obj);
    }

    public bool ContainsReceiver(BLEReceiver obj)
    {
        return bleReceivers.Contains(obj);
    }

    public List<BLEReceiver> Receivers => bleReceivers;


}
