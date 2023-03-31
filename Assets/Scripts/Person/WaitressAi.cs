using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(GenericPersonAi))]
public class WaitressAi : MonoBehaviour, IPerson
{
    PersonMovement personMovement;

    enum State
    {
        Idle,
        KitchenService,
        TableService,
        Leaving
    }

    [SerializeField]
    State state;
    float inState;

    const float serviceTime = 10;
    const float tableServiceTime = 10;

    [SerializeField]
    int servicePointIndex = 0;

    [SerializeField]
    WaitressService ws;

    [SerializeField]
    ServicePoint sp;

    [SerializeField, ReadOnly]
    bool destinationReached = false;

    [SerializeField]
    SimulationTime time;

    void Awake()
    {
        person = GetComponent<GenericPersonAi>();
        personMovement = GetComponent<PersonMovement>();
        time = FindObjectOfType<SimulationTime>();
    }

    void Start()
    {
        inState = time.time;
    }

    public GenericPersonAi person { get; private set; }

    public float Speed { set => personMovement.speed = value; }

    void FixedUpdate()
    {

        switch (state)
        {
            case State.Leaving:
                if (personMovement.remainingDistance <= .02f)
                {
                    person.OnEnterExit();
                }
                break;

            case State.Idle:

                if (person.leaveTime <= time.time)
                {
                    person.GoToExit();
                    state = State.Leaving;
                    return;
                }
                if (ws.QueueCount > 0)
                {
                    gotoQueueServicePoint();
                    break;
                }

                gotoKitchenServicePoint();
                break;

            case State.KitchenService:

                if (!destinationReached)
                {
                    servicePointReached();
                    break;
                }

                if (time.time - inState >= serviceTime)
                {
                    state = State.Idle;
                }

                break;
            case State.TableService:

                if (!destinationReached)
                {
                    servicePointReached();
                    break;
                }

                if (time.time - inState >= tableServiceTime)
                {
                    gotoKitchenServicePoint();
                }

                break;
        }
    }

    bool servicePointReached()
    {
        if (personMovement.remainingDistance <= .02f)
        {
            destinationReached = true;
            inState = time.time;
            sp.OnPointReached.Invoke();
            return true;
        }

        return false;
    }
    void gotoKitchenServicePoint()
    {
        destinationReached = false;
        state = State.KitchenService;
        sp = ws.servicePoints[servicePointIndex];
        servicePointIndex++;
        servicePointIndex %= ws.servicePoints.Length;
        person.MoveTo(sp.transform);
    }

    void gotoQueueServicePoint()
    {
        destinationReached = false;
        state = State.TableService;
        sp = ws.Dequeue();

        if (sp == null)
        {
            state = State.Idle;
        }

        person.MoveTo(sp.transform);
    }

    public bool OnEnterExit()
    {
        return true;
    }
}
