using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TrainEntry : MonoBehaviour
{

    const float squaredSeatingRange = 100f;     // 10m
    const float squaredStandingRange = 400f;    // 20m

    StandingArea[] standingAreas;
    [SerializeField]
    Seat[] seatsInRange;
    [SerializeField]
    List<Seat> freeSeats;
    public TrainLogic train;

    Collider2D entryCollider;

    public Bounds Bounds { get => entryCollider.bounds; }

    private void Awake()
    {
        train = GetComponentInParent<TrainLogic>();
        entryCollider = GetComponent<Collider2D>();
    }

    public void setSeats(Seat[] seats)
    {
        freeSeats = new List<Seat>();

        foreach (var seat in seats)
        {
            if ((transform.position - seat.transform.position).sqrMagnitude <= squaredSeatingRange)
            {
                freeSeats.Add(seat);
            }
        }

        seatsInRange = freeSeats.ToArray();
    }

    public void setStandingAreas(StandingArea[] areas)
    {
        var temp = new List<StandingArea>();

        foreach (var area in areas)
        {
            if ((Bounds.center - area.Bounds.center).sqrMagnitude <= squaredStandingRange)
            {
                temp.Add(area);
            }
        }

        standingAreas = temp.ToArray();
    }


    public Seat GetRandomSeat()
    {
        var index = Random.Range(0, seatsInRange.Length - 1);
        return seatsInRange[index];
    }


    public Seat GetRandomFreeSeat()
    {
        if (freeSeats.Count > 0 )
        {
            var index = Random.Range(0, freeSeats.Count - 1);
            var seat = freeSeats[index];
            freeSeats.RemoveAt(index);
            return seat;
        }

        return null;

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

    public Vector3 getStandingPoint()
    {
        var standingArea = standingAreas[Random.Range(0, standingAreas.Length - 1)];
        standingArea.Increase();
        return Utils.RandomPositionInBounds(standingArea.Bounds);
    }

    internal void LeftSeat(Seat seat)
    {
        freeSeats.Add(seat);
        seat.SeatingPerson = null;
    }

    internal void UpdateSeats()
    {
        foreach (var seat in seatsInRange)
        {
            if (freeSeats.Contains(seat) && seat.SeatingPerson != null)
            {
                freeSeats.Remove(seat);
            }

            if (!freeSeats.Contains(seat) && seat.SeatingPerson == null)
            {
                freeSeats.Add(seat);
            }
        }
    }

    internal void DisablePhysics()
    {
        entryCollider.enabled = false;
    }

    internal void EnablePhysics()
    {
        entryCollider.enabled = true;
    }
}
