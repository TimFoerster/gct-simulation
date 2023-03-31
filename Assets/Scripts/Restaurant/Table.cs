using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Table : MonoBehaviour
{

    enum State
    {
        Free,
        WaitingForSeating,
        WaitingForCards,
        CheckingCards,
        WantsToOrder,
        WaitingForFood,
        Eating,
        Done
    }

    // Reading 2 mins in cards
    const float checkingCardsTime = 120;
    const float waitingForFood = 240;

    [SerializeField]
    State state;

    float inState;


    public Seat[] seats;
    List<Seat> freeSeats;

    Restaurant restaurant;
    WaitressService ws;

    PersonGroup personGroup;

    [SerializeField]
    ServicePoint servicePoint;

    
    void Start()
    {
        restaurant = GetComponentInParent<Restaurant>();
        ws = GetComponentInParent<WaitressService>();
        freeSeats = seats.ToList();
    }

    private void FixedUpdate()
    {
        switch (state)
        {
            case State.CheckingCards:
                if (Time.time - inState >= checkingCardsTime)
                {
                    Ordering();
                }
                break;

            case State.WaitingForFood:
                if (Time.time - inState >= waitingForFood)
                {
                    personGroup.GetComponent<RestaurantPersonGroup>().ReadyToEat();
                    state = State.Eating;
                }
                break;
        }
    }

    public void Reservate(PersonGroup pg)
    {
        personGroup = pg;
        state = State.WaitingForSeating;
    }

    public void Leave()
    {
        state = State.Done;
        personGroup = null;
        freeSeats = seats.ToList();
        restaurant.OnTableLeft(this);
    }

    public bool IsFree { get => personGroup == null; }

    public Seat GetRandomFreeSeat(GenericPersonAi person)
    {
        if (freeSeats.Count == 0)
        {
            return null;
        }

        var index = restaurant.rng.NextInt(0, freeSeats.Count);
        var seat = freeSeats[index];
        freeSeats.RemoveAt(index);
        seat.SeatingPerson = person;
        return seat;
    }

    public void CheckSeat(Seat seat)
    {
        if (seat.SeatingPerson != null)
        {
            freeSeats.Remove(seat);
        }

        if (seat.SeatingPerson == null && !freeSeats.Contains(seat))
        {
            freeSeats.Add(seat);
        }
    }

    public void OrderingCards()
    {
        state = State.WaitingForCards;
        ws.Enqueue(servicePoint);
        servicePoint.OnPointReached.AddListener(WaitressHandingCards);
    }

    void WaitressHandingCards()
    {
        servicePoint.OnPointReached.RemoveListener(WaitressHandingCards);
        inState = Time.time;
        state = State.CheckingCards;
    }

    void Ordering()
    {
        state = State.WantsToOrder;
        ws.Enqueue(servicePoint);
        servicePoint.OnPointReached.AddListener(WaitressTakesOrders);
    }

    void WaitressTakesOrders()
    {
        servicePoint.OnPointReached.RemoveListener(WaitressHandingCards);
        inState = Time.time;
        state = State.WaitingForFood;
    }





}
