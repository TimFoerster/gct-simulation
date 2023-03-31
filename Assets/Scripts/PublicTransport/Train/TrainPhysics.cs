using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrainPhysics : MonoBehaviour
{
    Seat[] seats;
    StandingArea[] standingAreas;
    TrainEntry[] trainEntries;
    BoxCollider2D[] trainColliders;
    TrainLogic logic;
    public List<PublicTransportPersonAI> movingPersons;


    void Awake()
    {
        seats = GetComponentsInChildren<Seat>();
        standingAreas = GetComponentsInChildren<StandingArea>();
        trainEntries = GetComponentsInChildren<TrainEntry>();
        trainColliders = GetComponentsInChildren<BoxCollider2D>().Where(bc => bc.gameObject != this.gameObject).ToArray();
        logic = GetComponent<TrainLogic>();
    }

    private void Start()
    {
        // Set seats and Standing Areas
        foreach (var te in trainEntries)
        {
            te.setStandingAreas(standingAreas);
            te.setSeats(seats);
        }

    }

    internal void OnLeaving()
    {
        foreach (var te in trainEntries)
        {
            te.UpdateSeats();
            te.DisablePhysics();
        }

        foreach (var person in logic.passangers)
        {
            person.OnTrainStarting();
        }

        foreach (var coll in trainColliders)
        {
            coll.enabled = false;
        }

        movingPersons = null;
        currentPlatform = null;
    }

    StationPlatform currentPlatform;

    internal void OnArrive(StationPlatform platform)
    {
        currentPlatform = platform;
        foreach (var te in trainEntries)
        {
            te.EnablePhysics();
        }

        foreach (var coll in trainColliders)
        {
            coll.enabled = true;
        }

        platform.Station.UpdateMesh();
    }


    internal bool CanMove()
    {
        // Waiting everyone to sit/stand
        if (movingPersons == null)
        {
            movingPersons = new List<PublicTransportPersonAI>();
            foreach (var person in logic.passangers)
            {
                if (!person.IsReadyForMovingTrain())
                {
                    movingPersons.Add(person);
                }
            }
        }
        else
        {
            // filter out each person whom is not moving or left the train
            foreach (var person in movingPersons.ToList())
            {
                if (!logic.passangers.Contains(person) || person.IsReadyForMovingTrain())
                {
                    movingPersons.Remove(person);
                }
            }
        }

        return movingPersons.Count == 0;
    }

    internal void ForceMovement()
    {
        // if a person is still moving around, disable its agent
        foreach (var person in movingPersons)
        {
            person.OnForceTrainStart();
        }
        movingPersons.Clear();
    }

    public TrainEntry FindClosestTrainEntry(Vector3 position)
    {
        return Utils.GetClosestBoundingBox(trainEntries, position);
    }

    public Seat FindClosestSeat(Vector3 position)
    {
        return Utils.GetClosestBoundingBox(seats, position);
    }



}
