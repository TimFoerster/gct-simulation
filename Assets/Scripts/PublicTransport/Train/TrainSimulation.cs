using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrainSimulation : GenericSimulation
{

    struct PublicTransportSimulation: ISimulationOptions
    {
        public int trains;
    }

    [SerializeField] TrainLogic trainPrefab;

    [SerializeField] PublicTransportPersonAI personPrefab;

    [SerializeField] Sprite[] TrainProbeSprites;

    TrainWorld world;

    const int traincount = 4;
    readonly float rountripTime = 30 * 60;
    float trainDelay = 15f * 60f;


    protected override bool IsSimulationEnd()
    {
        return true;
    }

    public static int GetPersonCount(RandomNumberGenerator rng)
    {
        return rng.NextInt(26, 120 * traincount * 2);
    }

    protected override ISimulationOptions StartSimulation()
    {
        world = FindObjectOfType<TrainWorld>();
        var personCount = rng.NextInt(world.StationCount, 120 * traincount * 2);
        persons = new GenericPersonAi[personCount];

        simulationEnd = (traincount - 1) * trainDelay + rountripTime;

        for (int i = 0; i < personCount; i++)
            GeneratePerson(i);

        InitTrains();

        return new PublicTransportSimulation
        {
            trains = traincount * 2
        };
    }

    void GeneratePerson(int index)
    {
        var start = world.GetRandomStart(rng);
        var destination = world.GetRandomDestination(rng, start);
        var spawn = start.Spawns[rng.NextInt(start.Spawns.Length - 1)];

        var person = Instantiate(
            personPrefab,
            spawn.RandomPosition(rng),
            Quaternion.identity,
            start.Persons.transform);

        InitPerson(person);
        person.CurrentStation = start;
        person.DestinationStation = destination;
        person.person.spawnAt = rng.Range(0f, simulationEnd - rountripTime - 60);
        persons[index] = person.person;
    }

    protected override void SimulationFixedUpdate()
    {

    }

    protected override void Spawn()
    {
        
    }

    TrainSpawn[] trainSpawns;

    void InitTrains()
    {

        trainSpawns = FindObjectsOfType<TrainSpawn>();

        for(int i = 0; i < trainSpawns.Length; i++)
        {
            var trains = new TrainLogic[traincount];
            var spawn = trainSpawns[i];
            spawn.spawnInterval = trainDelay;

            var direction = spawn.direction;
            for (int t = 0; t < traincount; t++)
            {
                var train = Instantiate(
                     trainPrefab,
                     spawn.transform.position,
                     direction == TrainDirection.East ? Quaternion.identity : Quaternion.Euler(180, 180, 0),
                     spawn.trainsParent.transform
                 );

                trains[t] = train;
                train.gameObject.SetActive(false);
                // Alternating train index
                var tIndex = t * trainSpawns.Length + i;
                train.Init(direction, tIndex);
                train.SetPlatforms(spawn.platforms, spawn.TrackEnd);
                var sprite = TrainProbeSprites[tIndex % TrainProbeSprites.Length];
                foreach (var p in train.Probes)
                {
                    broadcastHandler.AddReceiver(p.receiver);
                    p.SetMeanSprite(sprite);
                }
            }

            spawn.trains = trains;
        }


    }

    protected override BLEProbe[] RegisterProbes()
    {
        return trainSpawns.SelectMany(s => s.trains.SelectMany(t => t.Probes)).ToArray();
    }

    protected override void ActivePersonDump(GenericPersonAi p)
    {
        base.ActivePersonDump(p);
        var ptp = p.GetComponent<PublicTransportPersonAI>();
        Logger.Log("PublicTransportPersonAI: "  + JsonUtility.ToJson(ptp, true) + "\nDestiniation: " + ptp.DestinationStation.name + "\nCurrent: " + ptp.CurrentStation.name);

    }
}
