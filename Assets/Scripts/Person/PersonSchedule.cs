using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;


[Serializable]
public class ScheduleType
{
    public float at;
    public float duration;
    public bool reached = false;
    public bool moving = false;
    public Scheduleable what;
    public Transform queuePosition;
    public float start;
    public float end = float.PositiveInfinity;
    public Transform consumeObject;
    public Vector3 where;

    public ScheduleType(float at, float duration, Scheduleable what)
    {
        this.at = at;
        this.duration = duration;
        this.what = what;
    }


    public ScheduleType(float at, float duration, Scheduleable what, Vector3 where)
    {
        this.at = at;
        this.duration = duration;
        this.what = what;
        this.where = where;
    }




}

[RequireComponent(typeof(GenericPersonAi))]
public class PersonSchedule : INeed
{
    [SerializeReference]
    public ScheduleType currentBreak = null;

    public ScheduleType[] breaks;

    public int breakIndex;

    GenericPersonAi person;
    PersonMovement personMovement;
    PersonFloorMovement floorMovement;

    Floor floor => floorMovement ? floorMovement.floor : null;

    [SerializeField]
    SimulationTime time;

    // Start is called before the first frame update
    void Awake()
    {
        person = GetComponent<GenericPersonAi>();
        floorMovement = GetComponent<PersonFloorMovement>();
        personMovement = GetComponent<PersonMovement>();
        time = FindObjectOfType<SimulationTime>();

    }

    private void FixedUpdate()
    {
        if (currentBreak == null) return;

        if (currentBreak.reached || !currentBreak.moving) return;

        if (!personMovement.destinationReached) return;

        currentBreak.reached = true;
        currentBreak.moving = false;

        currentBreak.what.OnRecrationEnter(this);
    }

    public override bool HasNeed() =>
        breakIndex < breaks.Length && breaks[breakIndex].at <= time.time;

    public override bool CanFullfillNeed() 
    {
        // rework when going to meeting
        // return person.floor == breaks[breakIndex].what.floor;
        return true;
    }

    public override void FullfillNeed()
    {
        // rework when going to meeting
        // Need to change level, if required

        currentBreak = breaks[breakIndex];
        if (floor != null && currentBreak.what.floor != floor)
        {
            floorMovement.GoToFloor(currentBreak.what.floor, GoToWhat);
            return;
        }

        GoToWhat();
    }

    void GoToWhat()
    {
        currentBreak.moving = true;
        if (currentBreak.where != default)
        {
            person.MoveTo(currentBreak.where, currentBreak.what.floor.number);
        } else
        {
            person.MoveTo(currentBreak.what.transform.position, currentBreak.what.floor.number);

        }
    }

    public override bool IsNeedCompleted() => currentBreak.reached && currentBreak.end > 0 && currentBreak.end <= time.time;

    public override void AfterNeedCompleted() {
        currentBreak.what.Consumed(this);
        breakIndex++;
        currentBreak = null;
    }

    internal void Full()
    {
        currentBreak.end = time.time;
    }

    public void Consume(Transform pos = null)
    {
        if (pos != null)
        {
            person.MoveTo(pos);
            currentBreak.moving = true;
        }

        currentBreak.start = time.time;
        currentBreak.end = time.time + currentBreak.duration;
    }

    public void MoveToQueuePosition(Transform pos)
    {
        person.MoveTo(pos);
    }
}
