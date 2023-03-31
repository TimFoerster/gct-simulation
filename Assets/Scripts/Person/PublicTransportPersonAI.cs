using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class PublicTransportPersonAI : MonoBehaviour, IPerson
{
    public Station CurrentStation;
    public Station DestinationStation;

    public TrainLogic train;

    public TrainEntry trainEntry;

    Seat seat;
    Vector3 standingPoint;

    [SerializeField]
    PersonMovement personMovement;

    [SerializeField]
    RandomNumberGenerator rng;

    public enum State
    {
        Spawned,
        GoingToPlatform,
        WaitingForTrain,
        EnterTrain,
        SearchingSeat,
        SearchingStandingPoint,
        Idle,
        GoingToSeat,
        GoingToStandpoint,
        Seating,
        Standing,
        LeavingTrain,
        Leaving
    }

    public enum Environment
    {
        Dynamic,
        Static
    }
    public Environment environment = Environment.Dynamic;

    public State state;
    public StationPlatform Platform;

    public GenericPersonAi person;

    public float Speed { set => personMovement.speed = value; }

    GenericPersonAi IPerson.person => person;

    void FixedUpdate()
    {
        switch (state)
        {
            case State.Spawned:
                Platform =
                    CurrentStation.Index < DestinationStation.Index ?
                    CurrentStation.platformN : CurrentStation.platformS;

                // Check if train is there
                if (Platform.CanEnterTrain())
                {
                    state = State.WaitingForTrain;
                } else
                {
                    state = State.GoingToPlatform;
                    
                    personMovement.SetDestination(
                        Utils.GetRandomPointInsideCollider(Platform.WaitingArea, rng, 0f),
                        0
                    );
                }

                break;

            case State.GoingToPlatform:

                if (personMovement.destinationReached || Platform.CanEnterTrain())
                    state = State.WaitingForTrain;
                break;

            case State.WaitingForTrain:

                if (!Platform.CanEnterTrain()) return;
                 
                train = Platform.train;
                var entry = train.FindClosestTrainEntry(transform.position);

                if (!entry)
                {
                    Logger.LogWarning("Person did not found an entrance to train", this);
                    break;
                }

                trainEntry = entry;
                personMovement.SetDestination(entry.Bounds.ClosestPoint(transform.position), 0);
                state = State.EnterTrain;

                break;

            case State.EnterTrain:

                if (trainEntry == null || !Platform.CanEnterTrain())
                {
                    state = State.Spawned;
                    personMovement.ResetPath();
                    break;
                }

                if (personMovement.destinationReached)
                    OnTrainEnter();

                break;

            case State.SearchingSeat:

                var possibleSeat = trainEntry.GetRandomFreeSeat();
                if (possibleSeat == null)
                {
                    state = State.SearchingStandingPoint;
                    break;
                }

                if (!personMovement.CanReachPosition(possibleSeat.transform.position))
                {
                    Logger.Log("Person ", this);
                    Logger.Log("can not reach seat", possibleSeat);
                    trainEntry.CheckSeat(possibleSeat);
                    break;
                }

                seat = possibleSeat;

                state = State.GoingToSeat;
                personMovement.SetDestination(seat.transform.position, 0);

                break;

            case State.GoingToSeat:

                if (personMovement.remainingDistance < 0.1)
                {
                    if (seat.SeatingPerson is null)
                    {
                        seat.SeatingPerson = person;
                    }
                    else if (seat.SeatingPerson != person)
                    {
                        seat = null;
                        state = State.SearchingSeat;
                    }
                }

                if (personMovement.destinationReached)
                {
                    state = State.Seating;
                    BeforeVehicleMovement();
                }

                break;


            case State.SearchingStandingPoint:
                standingPoint = trainEntry.getStandingPoint();
                state = State.GoingToStandpoint;
                personMovement.SetDestination(standingPoint, 0);
                break;

            case State.GoingToStandpoint:

                if (personMovement.destinationReached)
                    state = State.Standing;
                break;

            case State.LeavingTrain:
                if (personMovement.remainingDistance < 0.1)
                {
                    OnTrainLeft();
                    gotoExit();
                }
                break;

        }
    }

    bool LeaveTrain()
    {
        state = State.LeavingTrain;
        return person.MoveTo(trainEntry.Bounds.ClosestPoint(transform.position), 0);
    }

    void gotoExit()
    {
        float closestDist = float.MaxValue;
        
        foreach (var exit in DestinationStation.Exits)
        {
            var dist = Vector3.Distance(exit.transform.position, transform.position);

            if (dist < closestDist)
            {
                closestDist = dist;
                person.exit = exit;
            }
        }
        if (!person.exit)
        {
            Logger.LogWarning("No exit found at station", DestinationStation);
        }

        person.GoToExit(0);
        state = State.Leaving;
    }

    void OnTrainEnter()
    {
        train = trainEntry.train;
        state = State.SearchingSeat;
        Platform = null;
        train.OnPersonTrainEnter(this);
    }

    void OnTrainLeft()
    {
        if (train)
        {
            train.OnPersonTrainLeft(this);
            train = null;
        }
        transform.parent = DestinationStation.Persons.transform;
    }

    public bool IsReadyForMovingTrain()
    {
        return state == State.Seating || state == State.Standing;
    }

    public void OnForceTrainStart()
    {
        Logger.LogWarning(transform.name + " was forced to stop, prevoius state: " + state, this);

        if (state == State.Leaving || state == State.LeavingTrain)
        {
            OnTrainLeft();
            return;
        }

        state = State.Standing;
        BeforeVehicleMovement();
    }

    public void BeforeVehicleMovement()
    {
        personMovement.ResetPath();
    }

    public void AfterVehicleMovement()
    {
    }

    public void OnTrainStarting()
    {
        environment = PublicTransportPersonAI.Environment.Static;
    }

    public void OnTrainStopped(Station station)
    {
        environment = PublicTransportPersonAI.Environment.Dynamic;

        if (DestinationStation == station)
        {
            if (seat != null)
                trainEntry.LeftSeat(seat);
            seat = null;
            LeaveTrain();
        }
    }

    bool IPerson.OnEnterExit()
    {
        return true;
    }
}
