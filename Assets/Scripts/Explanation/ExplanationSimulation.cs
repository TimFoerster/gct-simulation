using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ExplanationSimulation : GenericSimulation
{
    [SerializeField] ExplanationPerson personPrefab;
    [SerializeField] Transform personParent;
    [SerializeField] Floor floor;
    [SerializeField] Restaurant restaurant;
    [SerializeField] ConferenceRoom room;
    [SerializeField] Foyer foyer;
    [SerializeField] Foyer discussion;
    [SerializeField] MeetingTable[] meetingTables;

    Chair[] meetingChairs;
    int nextMeetingChairIndex;

    Chair NextRandomFreeMeetingChair() => meetingChairs[nextMeetingChairIndex++];



    [System.Serializable]
    struct ExplanationSimulationSettings : ISimulationOptions
    {
         
    }


    protected override bool IsSimulationEnd()
    {
        return true;
    }

    protected override void SimulationFixedUpdate()
    {

    }

    protected override void Spawn()
    {
    }

    SpawnEdge[] spawns;
    ExplanationPerson[] personList;

    const float HOUR = 60f * 60f;
    // Time table
    // 1. Arrival and Meeting in foyer for an hour
    const float ARRIVE_TILL = 40f * 60f;

    // 2. Discussion Table for an hour
    const float DISCUSSION_START_FROM = 55f * 60f;
    const float DISCUSSION_START_TO = 65f * 60f;

    // 3. Conference for 90mins
    const float CONFERENCE_START_FROM = 1.8f * HOUR;
    const float CONFERENCE_START_TO = 2f * HOUR;
    const float CONFERENCE_END_FROM = 2.8f * HOUR;
    const float CONFERENCE_END_TO = 3f * HOUR;

    // 4. Eating for an hour
    const float EATING_START_FROM = 3.5f * HOUR;
    const float EATING_START_TO = 3.7f * HOUR;
    const float EATING_END_FROM = 4.5f * HOUR;
    const float EATING_END_TO = 4.7f * HOUR;

    // 5. Foyer and leave
    const float LEAVE_START = 4.7f * HOUR;
    const float LEAVE_END = 5f * HOUR;

    protected override ISimulationOptions StartSimulation()
    {
        simulationEnd = LEAVE_END;
        spawns = FindObjectsOfType<SpawnEdge>();
        meetingTables = discussion.GetComponentsInChildren<MeetingTable>();
        meetingChairs = meetingTables.SelectMany(t => t.chairs).OrderBy(_ => rng.NextInt()).ToArray();

        var maxPersonCount = System.Math.Min(System.Math.Min(
            restaurant.Seats.Length,
            meetingChairs.Length), room.Chairs.Length);

        var personCount = rng.NextInt(10, maxPersonCount);//restaurant.Tables.SelectMany(t => t.seats).Count();//rng.NextInt(30, );

        GeneratePersons(personCount);

        persons = personList.Select(p => p.person).ToArray();

        return new ExplanationSimulationSettings
        {
            
        };
    }

    void GeneratePersons(int n)
    {
        personList = new ExplanationPerson[n];
        for (int i = 0; i < n; i++)
        {
            personList[i] = GeneratePerson( i == 0);
        }
    }


    ExplanationPerson GeneratePerson(bool speaker)
    {
        var spawnIndex = 0;
        var spawn = spawns[spawnIndex];

        var person = Instantiate(
            personPrefab,
            spawn.RandomPosition(rng),
            default,
            personParent
        );

        InitPerson(person, GetRandomSpeed());
        person.person.exit = spawn.GetComponent<Exit>();
        person.person.spawnAt = rng.Range(0f, ARRIVE_TILL);

        person.foyerPlace = foyer.NextRandomFreeSeat();
        
        person.discussionAt = rng.Range(DISCUSSION_START_FROM, DISCUSSION_START_TO);
        person.discussionChair = NextRandomFreeMeetingChair();

        if (speaker)
        {
            person.isSpeaker = true;
            person.speakerPosition = room.SpeakerPosition.position;
            person.conferenceAt = CONFERENCE_START_FROM - 60f;
            person.conferenceEnd = CONFERENCE_END_TO + 60f;
        } else
        {
            person.conferenceAt = rng.Range(CONFERENCE_START_FROM, CONFERENCE_START_TO);
            person.conferenceChair = room.NextRandomFreeChair();
            person.conferenceEnd = rng.Range(CONFERENCE_END_FROM, CONFERENCE_END_TO);
        }

        person.restaurantSeat = restaurant.NextRandomFreeSeat();
        person.launchTimeStart = rng.Range(EATING_START_FROM, EATING_START_TO);
        person.launchTimeEnd = rng.Range(EATING_END_FROM, EATING_END_TO);

        person.leaveTime = rng.Range(LEAVE_START, LEAVE_END);

        return person;
    }

}
