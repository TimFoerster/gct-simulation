using System;
using System.Linq;
using Unity.Collections;
using UnityEngine;

[RequireComponent(typeof(PersonGroup))]
public class RestaurantPersonGroup : MonoBehaviour
{

    enum State
    {
        Init,
        GoingToRestaurant,
        WaitingForFreeTable,
        GoingToTable,
        Seating,
        Eating,
        Done,
        Leaving
    }


    [ReadOnly, SerializeField]
    State state;

    float inState;

    public PersonGroup group;
    Restaurant restaurant;
    Table table;

    public RestaurantPersonAi[] members { get; private set; }

    public void SetMembers(RestaurantPersonAi[] members)
    {
        this.members = members;
        group.SetMembers(members.Select(m => m.person).ToArray());
    }

    void Awake()
    {
        group = GetComponent<PersonGroup>();
    }

    int spawned = 0;
    public void MemberSpawned()
    {
        spawned++;

        if (spawned == members.Length)
        {
            FindRestaurant();
        }
    }

    void TargetReached()
    {
        switch (state)
        {
            case State.GoingToRestaurant:
                FindTable();
                break;
        }
    }

    void FindTable()
    {
        if (restaurant.MaxQueueLengthReached())
        {
            group.GoToExit();
            return;
        }
        table = restaurant.GetFreeTable(this);
        if (table == null)
        {
            ReserveTable();
            return;
        }

        state = State.GoingToTable;
        foreach(var member in members)
        {
            var seat = table.GetRandomFreeSeat(member.person);
            member.GoToSeat(seat);
        }

        transform.parent = table.transform;
    }

    void ReserveTable()
    {
        state = State.WaitingForFreeTable;
        inState = Time.time;

        var wp = restaurant.GetWaitingQueue(this);
        if (wp == null)
        {
            Leave();
            return;
        }

        group.MoveTo(wp.transform.position, wp.direction);

    }

    public void TableGotFree()
    {
        FindTable();
    }

    public void ReadyToEat()
    {
        state = State.Eating;
        foreach (var m in members)
        {
            m.ReadyToEat();
        }
    }

    void FindRestaurant()
    {
        restaurant = FindObjectOfType<Restaurant>();
        if (restaurant == null)
        {
            throw new System.Exception("No Restaurant found");
        }

        var entry = restaurant.FindClosestViewpoint(members[0].transform.position);

        if (entry == null)
        {
            Leave();
            return;
        }

        foreach (var member in members)
        {
            member.GoingToEntry();
        }
        group.MoveTo(entry.Bounds().center, Vector3.right);
        state = State.GoingToRestaurant;
    }

    internal void OnViewpointEnter(RestaurantPersonAi restaurantPersonAi)
    {
        foreach (var member in members)
        {
            if (!member.ReadyToEnterRestaurant())
            {
                return;
            }
        }

        FindTable();
    }

    void Leave()
    {

        foreach (var member in members)
        {
            member.OnLeave();
        }

        state = State.Leaving;
        group.GoToExit();

        if (table != null)
        {
            table.Leave();
        }

    }

    public void OnEatingDone(RestaurantPersonAi member)
    {
        foreach(var m in members)
        {
            if (!m.DoneWithEating())
            {
                return;
            }
        }

        state = State.Done;
        inState = Time.time;
    }


    internal void OnSeating(RestaurantPersonAi restaurantPersonAi)
    {
        foreach (var m in members)
        {
            if (!m.IsSeating())
            {
                return;
            }
        }

        state = State.Seating;
        table.OrderingCards();
    }


    private void FixedUpdate()
    {
        if (state == State.Done && Time.time - inState > 5)
        {
            Leave();
        }
    }


}