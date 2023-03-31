using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.AI;

public partial class TrainLogic : MonoBehaviour
{
    public float MaxWaitingTimeForPassangerToSit = 30f;

    public float waitingTime;
    public float inState;

    public GameObject Passangers;

    public BLEProbe[] Probes;

    public State state = State.Driving;

    public List<PublicTransportPersonAI> passangers;
    TrainPhysics physics;

    public int trainIndex;
    public TrainDirection direction;

    TrainMovement trainMovement;
    TrainUI ui;

    public Station Target { get => trainMovement.Target; }

    private void Awake()
    {
        trainMovement = GetComponent<TrainMovement>();
        physics = GetComponent<TrainPhysics>();
        ui = GetComponentInChildren<TrainUI>();

    }

    private void Start()
    {
        foreach (var p in Probes)
        {
            p.receiver.globalIndex = trainIndex;
        }
    }

    void FixedUpdate()
    {
        switch (state) {

            case State.OpenedDoors:

                if (Time.time - inState > waitingTime)
                {
                    state = State.ClosedDoors;
                    inState = Time.time;
                }
                break;


            case State.ClosedDoors:

                // Wait a moment to catch up all events
                if ((Time.time - inState) > .5f && trainMovement.state != TrainMovement.State.Leaving)
                {
                    trainMovement.LeaveStation();

                }
                // Check if Maximum Watiting time is exceeded
                if ((Time.time - inState) > MaxWaitingTimeForPassangerToSit)
                {
                    trainMovement.ForceLeave();
                }
                break;
        }
    }


    public bool DoorsOpen()
    {
        return state == State.OpenedDoors;
    }


    public TrainEntry FindClosestTrainEntry(Vector3 position)
    {
        return physics.FindClosestTrainEntry(position);
    }

    internal void OnLeftStation()
    {
        state = State.Driving;
        inState = Time.time;
    }

    public void OnPersonTrainEnter(PublicTransportPersonAI person)
    {
        passangers.Add(person);
        person.transform.parent = Passangers.transform;
        if (physics.movingPersons != null)
        {
            physics.movingPersons.Add(person);
        }
    }

    public void OnPersonTrainLeft(PublicTransportPersonAI person)
    {
        passangers.Remove(person);
    }

    internal void SetPlatforms(StationPlatform[] platforms, Vector3 end)
    {
        trainMovement.platforms = platforms;
        trainMovement.trackEnd = end;
    }

    public void OnArrived(StationPlatform platform)
    {
        state = State.OpenedDoors;
        inState = Time.time;
        waitingTime = Random.Range(10, 15);

        foreach (var person in passangers)
        {
            person.OnTrainStopped(platform.Station);
        }
    }

    private void OnDestroy()
    {
        // update camera
        /*
        foreach (var t in FindObjectsOfType<TrainLogic>())
        {
            if (t != this && t.direction == direction)
            {
                ui.UpdateCamera();
            }
        }*/
    }


    public void EndReached()
    {
        Logger.Log(transform.name + " reached End");
        ui.DetachMainCamera();
        foreach (var probe in Probes)
        {
            probe.enabled = false;
        }
        gameObject.SetActive(false);
        
        //trainIndex--;
        //ui.UpdateCamera();
    }

    public void Init(TrainDirection direction, int index) 
    {

        this.direction = direction;
        this.trainIndex = index;

        transform.name = "Train " + direction.ToString() + " " + trainIndex;
        UpdateUi();
    }

    void UpdateUi()
    {
        /*
        if (direction == TrainDirection.West)
        {
            Passangers
        }*/
        if (ui == null || direction == TrainDirection.Unknown) { return;  }

        ui.UpdateDirection(direction);
        ui.UpdateCamera();
    }

}
