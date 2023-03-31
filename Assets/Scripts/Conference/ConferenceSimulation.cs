using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ConferenceSimulation : GenericSimulation
{
    // Person to spawn
    [SerializeField] ConferencePerson prefab;
    // Parent object of person
    [SerializeField] Transform personParent;

    const float startTime = 11 * 60 * 60;        // 11am

    ConferenceRoom[] rooms;
    
    //                                             40 mins    60 mins
    readonly float[] talkDurations = new float[]{ 40f * 60f, 60f * 60f };
    readonly float[] talkSlots = new float[] { 
        30f * 60f,           // 11:30
        (60 + 50)  * 60f,    // 12:50
        (180 + 10) * 60f};   // 14:10

    Talk[][] roomSlots;
    List<Talk>[] talksBySlot;

    SpawnEdge[] spawns;
    float[] spawnProberbillities;
    private float talkChance;
    const int max_persons = 400;

    public static int GetPersonCount(RandomNumberGenerator rng)
    {
        return 0;
    }

    protected override bool IsSimulationEnd()
    {
        return true;
    }

    protected override void SimulationFixedUpdate() {}

    protected override void Spawn() {}

    protected override ISimulationOptions StartSimulation()
    {
        spawns = FindObjectsOfType<SpawnEdge>();
        var spawnSum = spawns.Sum(s => s.spawnRate);
        spawnProberbillities = new float[spawns.Length];
        float sum = 0;
        for(int i = 0; i < spawns.Length; i++)
        {
            sum += spawns[i].spawnRate / spawnSum;
            spawnProberbillities[i] = sum;
        }

        FindObjectOfType<GameTime>().Offset = startTime;
        rooms = FindObjectsOfType<ConferenceRoom>();

        roomSlots = new Talk[rooms.Length][];
        for (int i = 0; i < roomSlots.Length; i++)
        {
            roomSlots[i] = new Talk[talkSlots.Length];
        }

        talksBySlot = new List<Talk>[talkSlots.Length];

        for(int i = 0; i < talksBySlot.Length; i++)
        {
            talksBySlot[i] = new List<Talk>();
        }

        GenerateMeetings();
        var simOptions = GeneratePersons();

        leavePersonTimes = persons.Select(p => p.leaveTime).OrderBy(l => l).ToArray();

        nextLeaveAt = leavePersonTimes[0];
        simulationEnd = leavePersonTimes[leavePersonTimes.Length - 1] + 300;

        return simOptions;

    }

    [Serializable]
    struct ConferenceSimulationOptions: ISimulationOptions
    {
        public float talkChance;
        public float attendingRate;
    }

    void GenerateMeetings()
    {
        talkChance = rng.Range(.25f, 1);
        // Generate talks
        Logger.Log("TalkChance: " + talkChance, this);
        do
        {
            for (int roomIndex = 0; roomIndex < rooms.Length; roomIndex++)
            {
                var room = rooms[roomIndex];
                var hasTalk = false;
                do
                {
                    for (int slotIndex = 0; slotIndex < talkSlots.Length; slotIndex++)
                    {
                        if (roomSlots[roomIndex][slotIndex] != null)
                        {
                            hasTalk = true;
                            continue;
                        }
                        // chance that a slot will be not filled
                        if (rng.Range() > talkChance) continue;

                        var meeting = new Talk(
                           talkSlots[slotIndex],
                           talkDurations[rng.NextInt(talkDurations.Length)] + rng.Range(-300, 300),
                           room,
                           rng.Range(.5f, 1.5f) // be less or more excited
                        );

                        roomSlots[roomIndex][slotIndex] = meeting;
                        talksBySlot[slotIndex].Add(meeting);
                        hasTalk = true;
                    }

                } while (!hasTalk); // ensure that every room has at least one talk

                room.Talks = roomSlots[roomIndex];
            }

        } while (talksBySlot.Any(slot => slot.Count == 0)); // ensure that every slot has at least one meeting
        
        // Even distribution between chairs
        for(int i = 0; i < talksBySlot.Length; i++)
        {
            var slotsum = talksBySlot[i].Sum(t => t.PullFactor);
            float sum = 0;
            for(int j = 0; j < talksBySlot[i].Count; j++)
            {
                sum += talksBySlot[i][j].PullFactor / slotsum;
                talksBySlot[i][j].probability = sum;
            }
        }
    }


    ISimulationOptions GeneratePersons()
    {
        // Find areas
        var ousideAreas = FindObjectsOfType<IdleArea>().Where(area => area.areaType == IdleArea.AreaType.Outside).ToArray();
        // Percentage
        var attendingRate = rng.Range(.8f, 1);
        Logger.Log("AttendingRate: " + attendingRate, this);

        // Max amount of persons in this simulation is capped by chairs per slot.
        var min = rooms.Min(r => r.Chairs.Length);
        var maxPerSlot = talksBySlot.Max(slot => slot.Sum(t => t.freeChairs.Count));
        var personCount = rng.NextInt(10, System.Math.Min((int)(maxPerSlot / attendingRate), max_persons));
        persons = new GenericPersonAi[personCount];

        Logger.Log("#Person: " + personCount, this);

        var building = FindObjectOfType<ConferenceBuilding>();
        for (int i = 0; i < personCount; i++)
        {
            SpawnEdge spawn = null;
            var spawnNumber = rng.Range();
            var isSpeaker = false;

            for(int si = 0; si < spawns.Length; si++)
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
                personParent);

            InitPerson(person);

            // all spawns contains a exit
            person.person.exit = spawn.GetComponent<Exit>();
            persons[i] = person.person;

            if (spawn.spawnType == SpawnEdge.SpawnType.Inside)
            {
                person.welcomePosition = Utils.GetRandomPointInsideCollider(
                    building.Foyers(1)[rng.NextInt(building.Foyers(1).Length)].Collider, rng
                );
            } 
            else
            {
                person.welcomePosition = Utils.GetRandomPointInsideCollider(
                    ousideAreas[rng.NextInt(ousideAreas.Length)].Collider, rng
                );
            }

            person.talks = new TalkDate[talkSlots.Length];
            person.building = building;
            for (int slotIndex = 0; slotIndex < talksBySlot.Length && talksBySlot[slotIndex].Count > 0; slotIndex++)
            {
                // chance to not attend
                if (rng.Range() > attendingRate) continue;

                var talks = talksBySlot[slotIndex];

                // Find talk for slot
                var talkNumber = rng.Range();
                Talk talk = null;
                for (int ti = 0; ti < talks.Count; ti++)
                {
                    if (talkNumber >= talks[ti].probability && ti < talks.Count - 1)
                        continue;

                    talk = talks[ti];
                    break;
                }

                // first person is speaker, but only for a single talk
                if (!talk.HasSpeaker && !isSpeaker)
                {
                    person.talks[slotIndex] =
                        new TalkDate(
                                talk.at + rng.Range(-600, -120),       // between 10 or 2 mins earlier
                                talk.duration + rng.Range(120, 600), // leaves between 2 or 10 mins later
                                talk,
                                talk.SpeakerPosition,
                                true
                            );

                    talk.AddSpeaker(person);
                    isSpeaker = true;
                } 
                else 
                {
                    var chair = talk.freeChairs[rng.NextInt(talk.freeChairs.Count)];

                    person.talks[slotIndex] = 
                        new TalkDate(
                            talk.at + rng.Range(-300, -60),     // up to 5 mins earlier 
                            talk.duration + rng.Range(0, 300),  // sit up to 5 mins longer
                            talk,
                            chair.transform
                    );

                    talk.freeChairs.Remove(chair);
                }

                talk.attendances.Add(person);

                // no spawn date? => set it
                if (person.person.spawnAt == default)
                    person.person.spawnAt = talk.at + rng.Range(-1800, -300); // up to 30 min earilier

                // always set end date of talk as leave date.
                person.person.leaveTime = person.talks[slotIndex].end;

                // No chairs left => remove from talk slots
                if (talk.freeChairs.Count == 0)
                    talksBySlot[slotIndex].Remove(talk);
            }

            // Person has no talks => let them do sth else
            if (person.person.leaveTime == default)
            {
                // arrive before the last talk starts
                person.person.spawnAt = rng.Range(0, talkSlots.Last());
                // leave before that last possible talk ends
                person.person.leaveTime = rng.Range(person.person.spawnAt, talkSlots.Last() + talkDurations.Last());
            }

        }

        var log = "--- SCHEDULE ---";

        for (int s = 0; s < talkSlots.Length; s++)
        {
            var slotTime = talkSlots[s];
            var start = System.TimeSpan.FromSeconds(startTime + slotTime).ToString();

            log += "\nSlot " + start + ": ";
            for (int r = 0; r < rooms.Length; r++)
            {
                var room = rooms[r];
                var slot = room.Talks[s];
                if (slot != null)
                    log += "\t" + room.name + " ( " + slot.attendances.Count + " )";
                else
                    log += "\t" + "---none---";
            }
        }

        Logger.Log( log );

        return new ConferenceSimulationOptions
        {
            attendingRate = attendingRate,
            talkChance = talkChance
        };
    }

}
