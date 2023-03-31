using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[RequireComponent(typeof(GenericPersonAi))]
public class Bladder : INeed
{
    [SerializeField, ReadOnly]
    float value;

    [SerializeField, ReadOnly]
    float threashold;

    // In Liter
    float incPerSecond;

    // In Liter
    float decPerSecond;

    [SerializeField]
    float washingHandsDuration = 3f;

    bool washingHands;

    float inState = 0f;

    GenericPersonAi person;
    PersonMovement personMovement;

    Toilet toilet;
    Sink sink;

    enum State
    {
        Okay,
        NeedsToGo,
        GoingOnToilet,
        OnToilet,
        GoingToSink,
        WashingHands,
        Done
    }

    [SerializeField, ReadOnly]
    State state;

    // Start is called before the first frame update
    void Start()
    {
        person = GetComponent<GenericPersonAi>();
        personMovement = GetComponent<PersonMovement>();
        if (threashold == 0)
        {
            Init(GetComponent<RandomNumberGenerator>());
        }
    }

    public void Init(RandomNumberGenerator rng)
    {
        // 0 and 500ml
        value = rng.Range(0f, .5f);

        // between 250ml and 1L
        threashold = rng.Range(.250f, 1f);

        // between 15 ml and 50 ml
        decPerSecond = rng.Range(.015f, .050f);

        // between 1 and 2 liters daily / seconds (24h * 60m * 60s)
        incPerSecond = rng.Range(1f, 2.5f) / (float)(24 * 60 * 60);

        washingHands = rng.Range() <= 0.75;
    }

    public float WhenGoToToilet()
    {
        return threashold / incPerSecond - value;
    }

    public float Duration =>
        threashold / decPerSecond;

    private void FixedUpdate()
    {
        value += incPerSecond * Time.fixedDeltaTime;

        switch (state)
        {
            case State.Okay:
                if (value >= threashold)
                {
                    state = State.NeedsToGo;
                }
                break;

            case State.GoingOnToilet:
                if (personMovement.destinationReached)
                {
                    state = State.OnToilet;
                }
                break;
            case State.OnToilet:
                value -= decPerSecond * Time.fixedDeltaTime;
                if (value <= 0f)
                {
                    if (washingHands)
                    {
                        GoToSink();
                        if (!sink)
                        {
                            state = State.Done;
                            break;
                        } 

                        state = State.GoingToSink;
                        inState = Time.time;
                    } else
                    {
                        state = State.Done;
                    }
                }
                break;
            case State.GoingToSink:
                if (personMovement.destinationReached)
                {
                    state = State.WashingHands;
                    inState = Time.time + washingHandsDuration;
                }
                break;
            case State.WashingHands:
                if (inState <= Time.time)
                {
                    state = State.Done;
                }
                break;
        }
    }

    void GoToSink()
    {
        var sinks = FindObjectsOfType<Sink>();
        sink = Utils.FindClosestObject<Sink>(sinks, transform.position);

        if (sink == null)
        {
            Logger.LogError("No sink found");
            return;
        }

        personMovement.SetDestination(sink.transform);
    }

    void FindClosestToilet()
    {
        var toilets = FindObjectsOfType<Toilet>();
        var closest = Utils.FindClosestObject<Toilet>(toilets, transform.position);

        if (closest == null)
        {
            Logger.LogError("No toilet found");
            return;
        }

        toilet = closest;
    }

    public override bool HasNeed() => state == State.NeedsToGo;

    public override bool CanFullfillNeed() 
    {
        FindClosestToilet();
        return toilet != null;
    }

    public override void FullfillNeed()
    {
        state = State.GoingOnToilet;
        person.MoveTo(toilet.transform);
    }

    public override bool IsNeedCompleted() => state == State.Done;
    public override void AfterNeedCompleted() => state = State.Okay;
}
