using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;



public class RestaurantSimulation : GenericSimulation
{
    [SerializeField] RestaurantPersonGroup restaurantPersonGroupPrefab;
    [SerializeField] RestaurantPersonAi restaurantPersonPrefab;

    [SerializeField] DummyPersonGroup dummyGroupPrefab;
    [SerializeField] DummyPersonAi dummyPrefab;

    [SerializeField] WaitressAi[] waitresses;

    Floor floor;

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

    public static int GetPersonCount(RandomNumberGenerator rng)
    {
        return 0;
    }

    SpawnEdge[] spawns;
    float[] spawnProberbillities;
    float[] spawnDistribution;

    List<GenericPersonAi> personList;

    [System.Serializable]
    struct RestaurantSimulationOptions : ISimulationOptions
    {
        public int dummyCount;
        public int customerCount;
        public float group1;
        public float group2;
        public float group3;
        public float group4;

    }
    protected override ISimulationOptions StartSimulation()
    {
        // 2h = 60m * 60*s * 2h
        simulationEnd = 60f * 60f * 2f;
        //simulationEnd = 60f * 4f;

        personList = new List<GenericPersonAi>();

        foreach (var waitress in waitresses)
        {
            var gpai = waitress.person;

            InitApp(waitress);
            personList.Add(gpai);
            gpai.leaveTime = simulationEnd;
        }

        floor = FindObjectOfType<Floor>();

        spawns = FindObjectsOfType<SpawnEdge>();
        var spawnSum = spawns.Sum(s => s.spawnRate);
        spawnProberbillities = new float[spawns.Length];
        float sum = 0;
        for (int i = 0; i < spawns.Length; i++)
        {
            sum += spawns[i].spawnRate / spawnSum;
            spawnProberbillities[i] = sum;
        }

        // spawn distribution
        // 1: 30 - 80 %
        // 2: 10 - 50 %
        // 3: 1 - 5 %
        // 4: 0 - 5 %
        spawnDistribution = new float[4] { 
            rng.Range(30, 80),
            rng.Range(10, 50),
            rng.Range(1, 5),
            rng.Range(0, 5)
        };

        Debug.Log("Distributions: " + string.Join("|", spawnDistribution));

        spawnSum = spawnDistribution.Sum();
        sum = 0;
        for (int i = 0; i < spawnDistribution.Length; i++)
        {
            sum += spawnDistribution[i] / spawnSum;
            spawnDistribution[i] = sum;
        }

        var waitressCount = personList.Count;
        GenerateCustomers();
        var customerCount = personList.Count - waitressCount;
        GenerateDummies();
        var dummyCount = personList.Count - customerCount - waitressCount;
        persons = personList.ToArray();

        personList = null;

        Logger.Log("#" + dummyCount + " #" + customerCount);

        return new RestaurantSimulationOptions
        {
            dummyCount = dummyCount,
            customerCount = customerCount,
            group1 = spawnDistribution[0],
            group2 = spawnDistribution[1],
            group3 = spawnDistribution[2],
            group4 = spawnDistribution[3]
        };
    }

    void GenerateCustomers()
    {
        var min = simulationEnd / 60 / 60;
        var max = simulationEnd / 60 / 5;
        var n = rng.NextInt(min, max);
        for (int i = 0; i < n; i++)
        {
            GenereRestaurantGroup();
        }
    }

    void GenereRestaurantGroup()
    {
        var g = Instantiate(
                restaurantPersonGroupPrefab,
                floor.Persons
            );

        var members = GenerateGroupMembers(
            restaurantPersonPrefab, 
            g, 
            false,
            rng.Range(0, simulationEnd - 30 * 60),
            GetRandomSpeed());

        g.SetMembers(members);

        foreach (var member in members)
        {
            member.needsToEat = rng.Range(60 * 5, 60 * 20);
        }

    }

    void GenerateDummies()
    {
        var n = rng.NextInt(1, simulationEnd / 5);
        for (int i = 0; i < n; i++)
        {
            GenerateDummyGroup();
        }
    }

    DummyPersonGroup GenerateDummyGroup()
    {
        var dummyGroup = Instantiate(
                dummyGroupPrefab,
                floor.Persons
            );

        GenerateGroupMembers(
            dummyPrefab, 
            dummyGroup, 
            true, 
            rng.Range(0, simulationEnd - 60),
            GetRandomSpeed());

        return dummyGroup;
    }


    T[] GenerateGroupMembers<T, G>(T personPrefab, G group, bool otherExit, float spawnAt, float speed) where T: MonoBehaviour, IPerson where G : MonoBehaviour
    {
        var spawnIndex = GetRandomSpawnIndex();
        var spawn = spawns[spawnIndex];

        var memberCount = GetMemberCount();

        var members = new T[memberCount];

        for(int i = 0; i < memberCount; i++)
        {
            var person = Instantiate(
                personPrefab,
                spawn.RandomPosition(rng),
                default,
                group.transform
            );

            InitPerson(person, speed);
            if (otherExit)
                person.person.exit = spawns[(spawnIndex + 1) % spawns.Length].GetComponent<Exit>();
            else
                person.person.exit = spawn.GetComponent<Exit>();

            person.person.spawnAt = spawnAt;
            personList.Add(person.person);
            members[i] = person;

        }

        return members;
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

    int GetMemberCount()
    {
        var r = rng.Range();
        for (int ni = 0; ni < spawnDistribution.Length; ni++)
        {
            if (r < spawnDistribution[ni])
                return ni + 1;
        }

        return spawnDistribution.Length - 1;
    }

}
