using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public partial class TrainMovement : MonoBehaviour
{
    TrainLogic logic;

    public StationPlatform[] platforms;

    public int platformIndex = -1;

    // 100 km/h / 3.6 = 27.77 m/s
    public float TopSpeed = 27.77777777f;

    public Vector3 currentHaltingPoint;
    public Vector3 prevHaltingPoint;

    StationPlatform platform;
    public Vector3 trackEnd;

    public State state = State.Depot;

    float inState = 1;

    TrainPhysics physics;

    public Vector3 destination;
    public float speed = 0f;
    public float movement = 0f;
    // 1 m/s
    public float acceleration = 0.04f;

    Transform baseParent;
    TrainUI ui;

    // Start is called before the first frame update
    void Start()
    {
        logic = GetComponent<TrainLogic>();
        physics = GetComponent<TrainPhysics>();
        ui = GetComponentInChildren<TrainUI>();
        currentHaltingPoint = transform.position;
        inState = Time.time;
    }

    public float remainingDistance => (transform.position - destination).magnitude;
    public float remainingSqrDistance => (transform.position - destination).sqrMagnitude;

    private void FixedUpdate()
    {
        switch (state)
        {
            case State.Depot:

                // Sleep
                if (Time.time - inState < 1) { break; }

                // Check next stop
                if (!gotoNextStop())
                {
                    inState = Time.time;
                    break;
                }

                state = State.Driving;

                break;

            case State.Driving:

                var rd = remainingSqrDistance;

                if (rd < 0.01f)
                {
                    movement = 0f;
                    baseParent = transform.parent;
                    if (platform != null)
                    {
                        transform.parent = platform.transform;
                        physics.OnArrive(platform);
                    }
                    state = State.Arriving;
                    break;
                }

                if (rd < speed * speed * speed * 2.55f)
                {
                    movement = Mathf.Max(movement - acceleration * Time.fixedDeltaTime * 1.5f, .01f);
                }
                else
                {
                    movement = Mathf.Min(movement + acceleration * Time.fixedDeltaTime, 1);
                }

                speed = TopSpeed * movement;
                Vector2 pos = Vector2.MoveTowards(transform.position, destination, speed * Time.fixedDeltaTime);
                transform.position = pos;

                break;

            case State.Arriving:

                // End reached
                if (platform == null)
                {
                    logic.EndReached();
                    break;
                }

                state = State.Arrived;

                logic.OnArrived(platform);
                break;

            case State.Arrived:
                // Nothing to do
                break;
            case State.Leaving:

                if (!physics.CanMove()) { break; }
                if (Time.time - inState > 1)
                {
                    var prevPlatform = platform;
                    var nextStop = gotoNextStop();
                    if (nextStop == null && platform != null)
                    {
                        inState = Time.time;
                        break;
                    }
                    prevPlatform.TrainLeft();
                    physics.OnLeaving();
                    logic.OnLeftStation();
                    transform.parent = baseParent;
                    state = State.Driving;
                }

                break;
        }
    }

    StationPlatform gotoNextStop()
    {
        if (platformIndex + 1 == platforms.Length)
        {

            prevHaltingPoint = currentHaltingPoint;
            destination = trackEnd;
            currentHaltingPoint = platform.haltingPoint.transform.position;
            ui.NextStop(logic.trainIndex);
            platform = null;
            return null;
        }

        platform = platforms[platformIndex + 1];

        if (platform.isFree())
        {
            prevHaltingPoint = currentHaltingPoint;
            destination = platform.haltingPoint.transform.position;
            currentHaltingPoint = platform.haltingPoint.transform.position;
            ui.NextStop(logic.trainIndex, platform.Station);
            platform.IncomingTrain(logic);
            platformIndex++;

            return platform;
        }

        return null;

    }

    public void LeaveStation()
    {
        state = State.Leaving;
        platform.TrainLeaving();
    }

    public void ForceLeave()
    {
        physics.ForceMovement();
    }

    public void OnDestroy()
    {
        if (platform != null)
        {
            platform.TrainLeft();
        }
    }

    public Station Target { get => platform.Station; }
}
