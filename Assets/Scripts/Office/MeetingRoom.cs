using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class MeetingRoom : Scheduleable
{
    Chair[] chairs;
    public Chair[] Chairs => chairs;

    public Meeting[] ScheduledMeetings;

    List<Chair> freeChairs;
    protected override void ConsumeableAwake()
    {
        chairs = GetComponentsInChildren<Chair>();
        freeChairs = chairs.ToList();
    }

    public override void OnRecrationEnter(PersonSchedule recreation)
    {
        var chair = freeChairs.First();
        freeChairs.Remove(chair);
        recreation.Consume(chair.transform);
        recreation.currentBreak.consumeObject = chair.transform;
    }

    public override void Consumed(PersonSchedule recreation)
    {
        freeChairs.Add(recreation.currentBreak.consumeObject.GetComponent<Chair>());
    }

    protected override Collider2D GetCollider() => 
        GetComponentInChildren<EdgeCollider2D>();
}
