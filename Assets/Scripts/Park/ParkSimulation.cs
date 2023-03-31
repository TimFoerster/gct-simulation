using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ParkSimulation : GenericSimulation
{
    [SerializeField] ParkPerson parkPersonPrefab;
    [SerializeField] Transform parkPersons;
    [SerializeField] Floor floor;

    PathPoint[] pathPoints;
    ParkArea[] parkAreas;
    ParkMeeting[] parkMeetings;  

    const int maxPathLength = 10;
    const int maxPersonCountPerMeeting = 10;

    struct ParkSimulationSettings : ISimulationOptions
    {
        public int meetings;
    }

    public static int GetPersonCount(RandomNumberGenerator rng)
    {
        return rng.NextInt(100, 2000);
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
    float[] spawnProberbillities;
    float[] spawnDistribution;

    ParkPerson[] personList;

    protected override ISimulationOptions StartSimulation()
    {
        // 2h = 60m * 60*s * 2.5h
        simulationEnd = 60f * 60f * 2.5f;
        //simulationEnd = 60f * 4f;

        spawns = FindObjectsOfType<SpawnEdge>();
        pathPoints = FindObjectsOfType<PathPoint>();
        parkAreas = FindObjectsOfType<ParkArea>();

        var spawnSum = spawns.Sum(s => s.spawnRate);
        spawnProberbillities = new float[spawns.Length];
        float sum = 0;
        for (int i = 0; i < spawns.Length; i++)
        {
            sum += spawns[i].spawnRate / spawnSum;
            spawnProberbillities[i] = sum;
        }

        var personCount = rng.NextInt(100, 2000);
        Logger.Log("#" + personCount + " Persons");
        var parkMeetCount = rng.NextInt(20, personCount / 2);
        Logger.Log("#" + parkMeetCount + " Park meetings");

        GenerateParkMeets(parkMeetCount);
        GeneratePersons(personCount);

        persons = personList.Select(p => p.person).ToArray();


        return new ParkSimulationSettings
        {
            meetings = parkMeetings.Length
        };
    }

    void GenerateParkMeets(int count)
    {
        parkMeetings = new ParkMeeting[count];
        for(int i = 0; i < parkMeetings.Length; i++)
        {
            var start = rng.Range(10f, simulationEnd - 360f);
            var duration = rng.Range(300, simulationEnd - start);
            var pcount = rng.NextInt(maxPersonCountPerMeeting);
            parkMeetings[i] = new ParkMeeting
            {
                area = parkAreas[rng.NextInt(parkAreas.Length)],
                at = start,
                duration = duration,
                maxPersonCount = pcount,
                personCount = 0,
                persons = new ParkPerson[pcount]
            };
        }

        parkMeetings = parkMeetings.OrderBy(p => p.at).ToArray();
    }

    void GeneratePersons(int n)
    {
        personList = new ParkPerson[n];
        for (int i = 0; i < n; i++)
        {
            personList[i] = GeneratePerson();
        }
    }


    ParkPerson GeneratePerson()
    {
        var spawnIndex = GetRandomSpawnIndex();
        var otherExit = rng.Range() > 0.5;
        var spawn = spawns[spawnIndex];
        var spawnAt = rng.Range(0f, simulationEnd - 120f);
        var person = Instantiate(
            parkPersonPrefab,
            spawn.RandomPosition(rng),
            default,
            parkPersons
        );

        InitPerson(person, GetRandomSpeed());
        person.person.exit = otherExit ? spawns[GetRandomSpawnIndex()].GetComponent<Exit>() : spawn.GetComponent<Exit>();
        person.person.spawnAt = spawnAt;

        var hasMeeting = false;

        for(int i = 0; i < parkMeetings.Length; i++)
        {
            var meeting = parkMeetings[i];
            if (meeting.at + 300 < spawnAt) continue; // meeting is to far in past
            if (meeting.at > spawnAt) break; // meeting is later
            if (meeting.personCount == meeting.maxPersonCount) continue; // meeting is full

            hasMeeting = true;
            person.waypoints = new ParkWaypoint[1] { 
                new ParkWaypoint
                {
                    destination = Utils.RandomPositionInBounds(meeting.area.col.bounds),
                    transform = meeting.area.transform,
                    waitFor = meeting.duration
                } 
            };
            meeting.personCount++;

        }

        if (!hasMeeting)
        {
            person.waypoints = GenerateRandomPath();
            person.person.leaveTime = simulationEnd;
        }
        return person;
    }

    ParkWaypoint[] GenerateRandomPath()
    {
        var count = rng.NextInt(3, maxPathLength);
        var path = new ParkWaypoint[count];

        for(int i = 0; i < count; i++)
        {
            var point = pathPoints[rng.NextInt(pathPoints.Length)];

            path[i] = new ParkWaypoint
            {
                destination = new Vector3(
                        point.transform.position.x + rng.Range(-.5f, .5f),
                        point.transform.position.y + rng.Range(-.5f, .5f),
                        point.transform.position.z
                    ),
                transform = point.transform,
                waitFor = 0
            };

        }

        return path;

    }


    int GetRandomSpawnIndex()
    {
        var spawnNumber = rng.Range();

        for (int si = 0; si < spawns.Length; si++)
        {
            if (spawnNumber >= spawnProberbillities[si])
                continue;

            return si;
        }

        return spawnProberbillities.Length - 1;
    }

}
