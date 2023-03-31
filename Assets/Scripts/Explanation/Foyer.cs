using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(RandomNumberGenerator))]
public class Foyer : MonoBehaviour
{
    [SerializeField] Table[] tables;

    public Table[] Tables => tables;

    RandomNumberGenerator rng;

    Seat[] seats;
    int nextSeatIndex;


    private void Awake()
    {
        rng = GetComponent<RandomNumberGenerator>();
        tables = GetComponentsInChildren<Table>();
        seats = tables.SelectMany(t => t.seats).OrderBy(_ => rng.NextInt()).ToArray();
    }

    public Seat NextRandomFreeSeat()
    {
        var seat = seats[nextSeatIndex];
        nextSeatIndex++;
        return seat;
    }


}
