using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ConferencePerson : MonoBehaviour, IPerson
{
    enum ConferenceState
    {
        Arriving,
        WaitingForTalk,
        BetweenTalks,
        End
    }

    enum State
    {
        Init,
        Need,
        Comming,
        Idle,
        GoingToTalk,
        InTalk,
        Leaving,
        WalkAround
    }

    public GenericPersonAi person { get; set; }
    PersonMovement personMovement;

    [SerializeField]
    ConferenceState conferenceState = ConferenceState.Arriving;

    [SerializeField]
    State state = State.Init;

    [SerializeField]
    State prevState;
    Vector3 prevPosition;
    int prevFloor;


    public Vector3 welcomePosition;

    public TalkDate[] talks;

    int talkIndex;

    [SerializeField]
    float waitTill = 0;

    public ConferenceBuilding building;

    RandomNumberGenerator rng;

    [SerializeField]
    SimulationTime time;

    [SerializeField]
    Seat standingPosition;

    void Awake()
    {
        person = GetComponent<GenericPersonAi>();
        person.PersonInstance = this;
        personMovement = GetComponent<PersonMovement>();
        rng = GetComponent<RandomNumberGenerator>();
        time = FindObjectOfType<SimulationTime>();
    }


    public float Speed { set => personMovement.speed = value; }

    void FixedUpdate()
    {

        switch (state)
        {
            case State.Need:
                // Need is still active?
                if (person.HasNeed) return;
                // Return to previous position after need is completed
                person.MoveTo(prevPosition, prevFloor);
                state = prevState;
                break;

            case State.Init:

                // find first non null entry
                for (var ti = 0; ti < talks.Length; ti++)
                {
                    talkIndex = ti;
                    if (talks[ti] != null)
                        break;
                }

                if (welcomePosition != default)
                {
                    person.MoveTo(welcomePosition, 0);
                    state = State.Comming;
                    return;
                }

                state = State.Idle;
                break;

            case State.Comming:
                if (personMovement.destinationReached)
                {
                    state = State.Idle;
                    waitTill = time.time;
                }

                break;
            case State.Idle:
                if (person.CheckForNeeds() != null)
                {
                    prevPosition = personMovement.destination;
                    prevFloor = personMovement.currentFloor;

                    person.FullfillNeed();
                    prevState = State.Idle;

                    state = State.Need;
                    return;
                }

                if (HasTalk())
                {
                    if (standingPosition != null)
                    {
                        standingPosition.SeatingPerson = null;
                        standingPosition = null;
                    }

                    GoToTalk();
                    return;
                }

                if (person.leaveTime <= time.time)
                {
                    if (standingPosition != null)
                    {
                        standingPosition.SeatingPerson = null;
                        standingPosition = null;
                    }
                    state = State.Leaving;
                    person.GoToExit();
                    return;
                }

                // Walk Around
                if (waitTill < time.time)
                {
                    WalkAround();
                    return;
                }

                break;

            case State.WalkAround:
                if (personMovement.destinationReached)
                {
                    state = State.Idle;
                }
                break;

            case State.GoingToTalk:
                if (personMovement.destinationReached)
                    state = State.InTalk;
                break;

            case State.InTalk:
                if (!TalkDone) return;

                NextTalk();

                state = State.Idle;
                conferenceState = CurrentTalk == null ? ConferenceState.End : ConferenceState.BetweenTalks;

                break;
        }
    }

    TalkDate NextTalk()
    {
        talkIndex++;

        if (CurrentTalk is null && talkIndex < talks.Length)
        {
            return NextTalk();
        }

        return CurrentTalk;
    }

    void WalkAround()
    {
        if (CurrentTalk != null && CurrentTalk.at - 900 < time.time)
        {
            conferenceState = ConferenceState.WaitingForTalk;
        }

        state = State.WalkAround;
        switch (conferenceState)
        {
            // Search something in Foyer
            case ConferenceState.Arriving:
            case ConferenceState.End:
            case ConferenceState.BetweenTalks:
                personMovement.SetDestination(
                    Utils.GetRandomPointInsideCollider(
                        building.Foyers(1)[rng.NextInt(building.Foyers(1).Length)].Collider,
                        rng
                     ), 1
                );

                waitTill = time.time + rng.Range(5, 60);

                break;

            case ConferenceState.WaitingForTalk:

                if (standingPosition != null)
                {
                    standingPosition.SeatingPerson = null;
                    standingPosition = null;
                }

                // Find a free place
                var floor = CurrentTalk.talk.room.floor;
                var sp = building.StandingPositions(floor);
                var spIndex = rng.NextInt(sp.Length);
                var startindex = spIndex;

                while (sp[spIndex].SeatingPerson != null)
                {
                    spIndex = (spIndex + 1) % sp.Length;
                    if (spIndex == startindex)
                        throw new Exception("Not enaugh Standing Positions are avaiable for floor " + floor);
                }

                standingPosition = sp[spIndex];
                standingPosition.SeatingPerson = person;

                personMovement.SetDestination(
                     standingPosition.transform.position,
                     floor
                );

                waitTill = time.time + rng.Range(5 * 60, 60 * 60);

                break;
        }
    }
    TalkDate CurrentTalk =>
        talkIndex < talks.Length &&
        talks[talkIndex] != null &&
        talks[talkIndex].at > 0 ? 
        talks[talkIndex] : 
        null;

    bool HasTalk() =>
        CurrentTalk != null && CurrentTalk.at <= time.time;
    
    void GoToTalk()
    {
        state = State.GoingToTalk;
        person.MoveTo(talks[talkIndex].position);
    }

    bool TalkDone =>
        CurrentTalk != null && CurrentTalk.end <= time.time;

    public bool OnEnterExit()
    {
        return state == State.Leaving;
    }
}
