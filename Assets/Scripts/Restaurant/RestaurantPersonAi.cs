using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(GenericPersonAi))]
public class RestaurantPersonAi : MonoBehaviour, IPerson
{
    enum State {
        Init,
        GoingToRestaurant,
        WaitingToEnter,
        GoingToSeat,
        Seating,
        Eating,
        Done,
        Leaving,
        Need
    }


    RestaurantPersonGroup group;

    PersonMovement personMovement;

    [ReadOnly, SerializeField]
    State state;

    public float needsToEat;

    Seat seat;

    public GenericPersonAi person { get; private set; }

    public float Speed { set => personMovement.speed = value; }

    private void Awake()
    {
        personMovement = GetComponent<PersonMovement>();
        person = GetComponent<GenericPersonAi>();
        group = GetComponentInParent<RestaurantPersonGroup>();
        state = State.Init;
    }

    // Start is called before the first frame update
    void Start()
    {
        group.MemberSpawned();
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        switch (state)
        {
            case State.Need:
                if (person.HasNeed == null || person.HasNeed.IsNeedCompleted())
                {
                    state = State.Eating;
                    person.MoveTo(seat.transform);
                }
                break;
            case State.GoingToRestaurant:
                if (personMovement.remainingDistance <= .02)
                {
                    onViewpointReached();
                }
                break;
            case State.GoingToSeat:
                if (personMovement.remainingDistance <= .02)
                {
                    state = State.Seating;
                    group.OnSeating(this);
                }
                break;

            case State.Eating:
                needsToEat -= Time.fixedDeltaTime;

                if (needsToEat <= 0)
                {
                    state = State.Done;
                    group.OnEatingDone(this);
                    needsToEat = 0f;
                }

                var need = person.CheckForNeeds();
                if (need != null)
                {
                    state = State.Need;
                    person.FullfillNeed();
                }

                break;
        }
    }

    public void GoToSeat(Seat seat)
    {
        state = State.GoingToSeat;
        person.MoveTo(seat.transform);
        this.seat = seat;
    }

    public bool DoneWithEating() => state == State.Done;

    public bool ReadyToEnterRestaurant() => state == State.WaitingToEnter;

    internal void ReadyToEat()
    {
        state = State.Eating;
    }

    internal void onViewpointReached()
    {
        if (state != State.GoingToRestaurant)
        {
            return;
        }

        state = State.WaitingToEnter;
        group.OnViewpointEnter(this);
    }

    internal void GoingToEntry()
    {
        state = State.GoingToRestaurant;
    }

    internal bool IsSeating() => state == State.Seating;

    internal void OnLeave()
    {
        state = State.Leaving;
        seat.SeatingPerson = null;
        seat = null;
    }

    public bool OnEnterExit()
    {
        return true;
    }
}
