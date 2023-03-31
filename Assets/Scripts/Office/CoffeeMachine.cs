using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CoffeeMachine : Scheduleable
{
    [SerializeField]
    Transform[] queuePositions;

    [SerializeField]
    List<Transform> freeQueuePoints;

    [SerializeField]
    List<PersonSchedule> queue;

    bool isFree = true;
    PersonSchedule consumer;


    protected override void ConsumeableAwake()
    {
        freeQueuePoints = queuePositions.ToList();
    }

    public override void OnRecrationEnter(PersonSchedule recreation)
    {
        if(freeQueuePoints.Count == 0)
        {
            recreation.Full();
            return;
        }

        if (!isFree)
        {
            queue.Add(recreation);
            var pos = freeQueuePoints.First();
            recreation.currentBreak.queuePosition = pos;
            recreation.MoveToQueuePosition(pos);
            freeQueuePoints.RemoveAt(0);
            return;
        }

        isFree = false;
        recreation.Consume(transform);
        consumer = recreation;
        return;
    }

    public override void Consumed(PersonSchedule recreation)
    {
        if (consumer == recreation)
        {
            isFree = true;
            consumer = null;
        }
        else 
        if(recreation.currentBreak.queuePosition != null)
        {
            freeQueuePoints.Add(recreation.currentBreak.queuePosition);
        }

        queue.Remove(recreation);

        if (queue.Count == 0) return;

        var inQueue = queue.First();
        queue.Remove(inQueue);

        inQueue.Consume(transform);        
    }

    protected override Collider2D GetCollider()
    {
        return GetComponentInChildren<CapsuleCollider2D>();
    }
}
