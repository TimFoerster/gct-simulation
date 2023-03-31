using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StationPlatform : MonoBehaviour
{

    [SerializeField] public Transform haltingPoint;
    [SerializeField] public Station Station;
    public TrainLogic train;

    public enum State
    {
        Free,
        Awaiting,
        Arrived,
        Leaving
    }

    public State state = State.Free;

    public BoxCollider2D WaitingArea;

    public bool isFree()
    {
        return state == State.Free;
    }

    public bool CanEnterTrain()
    {
        return state == State.Arrived && train != null;
    }


    void FixedUpdate()
    {
        switch (state)
        {
            case State.Awaiting: 
                if (train.DoorsOpen())
                {
                    state = State.Arrived;
                }
                break;

        }
    }

    public void IncomingTrain(TrainLogic train)
    {
        this.train = train;
        state = State.Awaiting;
    }

    public void TrainLeaving()
    {
        state = State.Leaving;
    }

    public void TrainLeft()
    {
        state = State.Free;
        train = null;
    }

}
