using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[RequireComponent(typeof(GenericPersonAi))]
public class ExplanationPerson : StatePerson<ExplanationPerson>
{
    [SerializeField]
    public override GenericPersonAi person { get; set; }
    [SerializeField]
    public override PersonMovement personMovement { get; set; }

    public Seat foyerPlace;
    public Chair discussionChair;
    public Chair conferenceChair;
    public Vector3 speakerPosition;
    public bool isSpeaker;

    public Seat restaurantSeat;

    public float discussionAt;
    public float conferenceAt;
    public float conferenceEnd;

    public float launchTimeStart;
    public float launchTimeEnd;
    public float leaveTime;

    [SerializeField]
    SimulationTime time;

    void Awake()
    {
        person = GetComponent<GenericPersonAi>();
        person.PersonInstance = this;
        personMovement = GetComponent<PersonMovement>();
        time = FindObjectOfType<SimulationTime>();
    }


    void Start()
    {
        InitWithState(this, time, 
            new MoveAndWaitState(foyerPlace.transform.position, discussionAt, 
            new MoveAndWaitState(discussionChair.transform.position, conferenceAt, 
            new MoveAndWaitState(isSpeaker ? speakerPosition : conferenceChair.transform.position, conferenceEnd,
            new MoveAndWaitState(discussionChair.transform.position, launchTimeStart, 
            new MoveAndWaitState(restaurantSeat.transform.position, launchTimeEnd, 
            new MoveAndWaitState(foyerPlace.transform.position, leaveTime, new LeaveState())
            )))))
        );
    }

}
