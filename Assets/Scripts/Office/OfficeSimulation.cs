using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class OfficeSimulation : GenericSimulation
{

    [SerializeField] OfficePerson prefab;

    [SerializeField] OfficeBuilding building;
    [SerializeField] Transform spawnParent;

    // 6 am = 6 * 60m * 60s
    const float startTime = 6 * 60 * 60;
    
    const float startingSpawnAt = 0f;

    // dont spawn after 11 am => 4hours after begin => 5 = 5h * 60m * 60s
    const float endingSpawnAt = 5 * 60 * 60;
    //const float endingSpawnAt = 1 * 60 * 60;
    const float minWorkingTime = 7 * 60 * 60;
    //const float minWorkingTime = 1 * 60 * 60;
    const float maxWorkingTime = 9 * 60 * 60;
    //const float maxWorkingTime = 2 * 60 * 60;

    MeetingRoom[] meetingRooms;
    int meetingIndex;
    float nextMeetingAt;
    Meeting[] meetings;

    [Serializable]
    struct OfficeSimulationOptions: ISimulationOptions
    {
        public int meetings;
    }

    public static int GetPersonCount(RandomNumberGenerator rng)
    {
        const int meetingRooms = 8;
        var numberOfMeetings = rng.NextInt(meetingRooms / 2, meetingRooms * 4);
        var room = rng.NextInt(0, meetingRooms);
        for (int meetingIndex = 0; meetingIndex < numberOfMeetings; meetingIndex++)
        {
            var at = (meetingIndex < numberOfMeetings / 2 ?
                // am / pm               * 15min slots * 60 seconds   
                rng.NextInt(8, 20) * 15 * 60 :
                rng.NextInt(28, 36) * 15 * 60);
            var duration = rng.Range(5 * 60, 60 * 60);
        }

        return rng.NextInt(140 / 8, 140);
    }

    protected override ISimulationOptions StartSimulation()
    {
        FindObjectOfType<GameTime>().Offset = startTime;
        meetingRooms = FindObjectsOfType<MeetingRoom>();

        try
        {
            generateMeetings();
        } catch (System.Exception e)
        {
            Logger.LogError("Simulation generation failed: " + e.Message, this);
            Application.Quit(1);
        }
        var options = CalculateSpawns();
        nextMeetingAt = meetings.Length > 0 ? meetings[0].at : float.PositiveInfinity;
        // end of simulation after last person is done + 1h buffer
        simulationEnd = endingSpawnAt + maxWorkingTime + 60 * 60;

        return options;
    }

    protected override void Spawn()
    {

    }

    protected override void SimulationFixedUpdate()
    { 
        while (time.time >= nextMeetingAt)
        {
            meetingIndex++;
            if (meetingIndex >= meetings.Length)
                nextMeetingAt = float.PositiveInfinity;
            else
                nextMeetingAt = meetings[meetingIndex].at;
        }
    }

    protected override bool IsSimulationEnd() => 
        float.IsPositiveInfinity(nextLeaveAt) && allGone;


    protected override void UiUpdate()
    {

        if (!float.IsInfinity(nextMeetingAt))
        {
            infoText.text += "\nNext meeting in " + System.TimeSpan.FromSeconds((int)(nextMeetingAt - time.time)).ToString();
        }       
    }

    void generateMeetings()
    {
        var numberOfMeetings = rng.NextInt(meetingRooms.Length / 2, meetingRooms.Length * 4);
        meetings = new Meeting[numberOfMeetings];

        var roomMeetingDict = new Dictionary<MeetingRoom, List<Meeting>>();
        for(int meetingIndex = 0; meetingIndex < numberOfMeetings; meetingIndex++)
        {
            var room = meetingRooms[rng.NextInt(0, meetingRooms.Length)];
            var meeting = new Meeting(
                   (float)(meetingIndex < numberOfMeetings / 2 ?
                        // am / pm               * 15min slots * 60 seconds   
                       rng.NextInt(8, 20) * 15 * 60 :
                       rng.NextInt(28, 36) * 15 * 60),
                   rng.Range(5*60, 60*60),
                   room.Chairs.Length,
                   0,
                   room
            );


            if (roomMeetingDict.ContainsKey(room)) {

                bool conflictFound = false;
                foreach(var m in roomMeetingDict[room])
                {
                    // earlier
                    if (meeting.at < m.at && meeting.end < m.at)
                        continue;

                    // later
                    if (m.end < meeting.at && m.end < meeting.end)
                        continue;

                    //within
                    conflictFound = true;
                    break;
                }

                if (!conflictFound)
                {
                    roomMeetingDict[room].Add(meeting);
                    meetings[meetingIndex] = meeting;
                }
            }
            else
            {
                roomMeetingDict.Add(room, new List<Meeting>() { meeting });
                meetings[meetingIndex] = meeting;
            }
        }

        meetings = meetings.Where(m => m != null).OrderBy(m => m.at).ToArray();

        foreach (var meeting in roomMeetingDict)
        {
            meeting.Key.ScheduledMeetings = meeting.Value.OrderBy(m => m.at).ToArray();
        }
    }
    OfficeSimulationOptions CalculateSpawns()
    {
        // Init
        var workspaces = building.Floors.SelectMany(f => f.workspaces).ToList();
        var workspacesCount = workspaces.Count();

        // Random number of persons with a given seed
        var personCount = rng.NextInt(workspacesCount / 8, workspacesCount);
        persons = new GenericPersonAi[personCount];

        var exits = FindObjectsOfType<Exit>();

        // Coffee machines
        var cmDict = new Dictionary<int, CoffeeMachine[]>();
        foreach(var floor in building.Floors)
        {
            cmDict.Add(floor.number, floor.GetComponentsInChildren<CoffeeMachine>());
        }


        var spawns = FindObjectsOfType<SpawnEdge>();
        var spawnSum = spawns.Sum(s => s.spawnRate);
        var spawnProberbillities = new float[spawns.Length];
        float sum = 0;
        for (int i = 0; i < spawns.Length; i++)
        {
            sum += spawns[i].spawnRate / spawnSum;
            spawnProberbillities[i] = sum;
        }


        for (int i = 0; i < spawnPersonCount; i++)
        {

            SpawnEdge spawn = null;
            var spawnNumber = rng.Range();

            for (int si = 0; si < spawns.Length; si++)
            {
                if (spawnNumber >= spawnProberbillities[si] && si < spawns.Length - 1)
                    continue;

                spawn = spawns[si];
                break;
            }

            var person = Instantiate(
                prefab,
                spawn.RandomPosition(rng),
                Quaternion.identity,
                spawnParent);

            InitPerson(person);

            person.person.exit = exits[rng.NextInt(0, exits.Length)];
            var workspaceIndex = rng.NextInt(0, workspaces.Count);
            person.workspace = workspaces[workspaceIndex];

            var floorNumber = person.workspace.floor.number;
            workspaces.RemoveAt(workspaceIndex);

            person.person.spawnAt = RandomFromDistribution.RandomRangeNormalDistribution(
                    rng, startingSpawnAt, endingSpawnAt, 0
                );

            person.person.leaveTime = person.person.spawnAt + rng.Range(minWorkingTime, maxWorkingTime);


            var recreation = person.GetComponent<PersonSchedule>();

            var breaks = new List<ScheduleType>();
            // meetings
            do
            {
                int meetingIndex = rng.NextInt(-meetings.Length, meetings.Length);

                if (meetingIndex < 0) break;

                ref var meeting = ref meetings[meetingIndex];
                if (meeting.currentNumberOfAttendence >= meeting.maxNumberOfAttendence) break;
                if (meeting.at < person.person.spawnAt || meeting.end > person.person.leaveTime) continue;
                meeting.currentNumberOfAttendence++;
                breaks.Add(
                    new ScheduleType(
                        meeting.at,
                        meeting.duration,
                        meeting.room,
                        Utils.RandomPositionInBounds(meeting.room.Collider.bounds, rng)
                        )
                    );

            } while (true);

            // closest coffemachine to workspace
            var cm = cmDict[floorNumber]
                .OrderBy(cm => (cm.transform.position - person.workspace.transform.position).sqrMagnitude)
                .First();

            int numberOfCmVisits = rng.NextInt(1, 5);
            for (int breakIndex = 0; breakIndex < numberOfCmVisits; breakIndex++)
            {
                breaks.Add(
                    new ScheduleType(
                        rng.Range(person.person.spawnAt + 60 * 5 * breakIndex, person.person.leaveTime - 60 * 30 * breakIndex), 
                        rng.Range(30, 120), 
                        cm,
                        Utils.RandomPositionInBounds(cm.Collider.bounds, rng)
                    )
                );
            }
            recreation.breaks = breaks.OrderBy(b => b.at).ToArray();
            persons[i] = person.person;
        }


        meetings = meetings.Where(m => m.currentNumberOfAttendence > 0).OrderBy(m => m.at).ToArray();

        Logger.Log("#meetings: " + meetings.Length);

        return new OfficeSimulationOptions
        {
            meetings = meetings.Length
        };
    }

}
