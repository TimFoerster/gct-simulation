using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.Profiling;
using UnityEngine;

[RequireComponent(typeof(SimulationRandomNumberGenerator))]
[RequireComponent(typeof(SimulationSychronizer))]
abstract public class GenericSimulation : MonoBehaviour, SimulationPreview
{
    [SerializeField]
    protected SimulationSychronizer simulationSychronizer;

    [SerializeField]
    TextAsset firstNamesAsset;

    [SerializeField]
    TextAsset surnameAssets;


    List<string> firstNames;
    List<string> surnames;

    [SerializeField] protected TMP_Text infoText;
    [SerializeField] AutoSpeed autoSpeed;

    private int spawnIndex;

    protected float nextSpawnAt;

    protected float[] leavePersonTimes;
    private int leaveIndex;
    protected float nextLeaveAt;

    [SerializeField]
    protected SimulationRandomNumberGenerator rng;

    [SerializeField]
    protected List<GenericPersonAi> activePersons;

    [SerializeField]
    protected List<GenericPersonAi> gonePersons;

    protected float uiUpdate;

    protected float simulationEnd;
    private float simulationEndWithBuffer;

    int personCleanupIndex = 0;

    [SerializeField]
    protected SimulationSychronizer sychronizer;

    protected GenericPersonAi[] persons;

    protected int spawnPersonCount => persons.Length;

    [SerializeField]
    protected BroadcastHandler broadcastHandler;

    [SerializeField]
    AppHandler appHandler;

    [SerializeField]
    PersonMovementHandler personMovementHandler;

    [SerializeField]
    protected SimulationTime time;

    [SerializeField]
    bool hasError = false;

    /*
    float nextMemoryPrint;
    const float memoryPrintInterval = 60f;

    float nextMemDump;
    const float memDumpInterval = 300f;
    */

    void Awake()
    {
        rng = GetComponent<SimulationRandomNumberGenerator>();

        senderTimeslotIndex = 0;
        /*
        nextMemoryPrint = memoryPrintInterval;
        nextMemDump = memDumpInterval;
        */
        time = FindObjectOfType<SimulationTime>();
    }

    ProfilerRecorder _totalReservedMemoryRecorder;

    void OnEnable()
    {
        _totalReservedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Reserved Memory");
    }

    void OnDisable()
    {
        _totalReservedMemoryRecorder.Dispose();
    }

    protected int personIndex;

    // Start is called before the first frame update
    void Start()
    {
        Debug.Log("Seed: " + SimulationSettings.Instance.Seed.ToString());
        Debug.Log("Scenario RNG Seed: " + rng.Seed.ToString());

        LoadingNames();
        autoSpeed.Enable();
        autoSpeed.slowDownAt = simulationEnd;
        spawnIndex = 0;
        leaveIndex = 0;
        var simulationOptions = StartSimulation();

        personMovementHandler.SetPersons(
            persons.Select(s => s.GetComponent<PersonMovement>()).ToArray()
        );

        sychronizer.SimulationEnd = simulationEnd;
        broadcastHandler.SetPersons(persons);
        appHandler.SetDevicesCount(persons.Length);


        var probes = RegisterProbes();

        foreach (var person in persons)
        {
            person.receiver.SetSenderCount(persons.Length);
            person.sender.SetReceiverCount(persons.Length + probes.Length);
        }

        // Static probes
        foreach (var probe in probes)
        {
            if (probe.GetComponent<BLESender>() == null)
                probe.receiver.SetSenderCount(persons.Length);
        }


        persons = persons.OrderBy(p => p.spawnAt).ToArray();
        nextSpawnAt = persons[0].spawnAt;
        leavePersonTimes = persons.Select(p => p.leaveTime).OrderBy(l => l).ToArray();
        nextLeaveAt = leavePersonTimes[0];

        _ = simulationSychronizer.RegisterSimulationAsync(persons.Length, simulationOptions);

        simulationEndWithBuffer = simulationEnd + 60 * 30;
    }

    protected virtual BLEProbe[] RegisterProbes()
    {
        return FindObjectsOfType<BLEProbe>();
    }

    int senderTimeslotIndex = 0;
    int appTimeslotIndex = 0;

    protected void InitApp<T>(T person) where T : MonoBehaviour, IPerson
    {
        InitApp(person, GetRandomSpeed());
    }


    protected void InitApp<T>(T person, float speed) where T : MonoBehaviour, IPerson
    {
        SetRandomName(personIndex, person);
        person.person.SetPersonIndex(personIndex);

        person.person.sender.Timeslot = senderTimeslotIndex;
        person.person.sender.uuid = personIndex;
        senderTimeslotIndex = (senderTimeslotIndex + 1) % broadcastHandler.maxSenderTimeslot;

        person.person.GetComponent<TracingApp>().Timeslot = appTimeslotIndex;
        appTimeslotIndex = (appTimeslotIndex + 1) % appHandler.maxIntervalIndex;

        var bladder = person.GetComponent<Bladder>();

        if (bladder != null)
            InitBladder(person, bladder, rng);

        person.Speed = speed;

        person.person.Init(broadcastHandler, appHandler, personMovementHandler, time);

        person.person.simulation = this;
        personIndex++;

    }

    internal void OnPersonLeft(GenericPersonAi genericPersonAi)
    {
        activePersons.Remove(genericPersonAi);
        gonePersons.Add(genericPersonAi);
        CleanupReset();
        LogState();
    }

    void LogState()
    {
        Logger.Log("Simulation: " + activePersons.Count + " persons active, " + gonePersons.Count + " waiting for sync");
    }

    protected float GetRandomSpeed() =>
        RandomFromDistribution.RandomRangeNormalDistribution(rng, 0.83f, 1.6f);

    protected void InitPerson<T>(T person, float speed) where T : MonoBehaviour, IPerson
    {
        person.gameObject.SetActive(false);
        InitApp(person, speed);
    }
    protected void InitPerson<T>(T person) where T : MonoBehaviour, IPerson
    {
        person.gameObject.SetActive(false);
        InitApp(person);
    }

    protected interface ISimulationOptions { };

    protected abstract ISimulationOptions StartSimulation();


    void FixedUpdate()
    {
        /*
        if (nextMemDump <= time.time)
        {
            int sum = activePersons.Sum(p => p.sender.Recorder.Count + p.receiver.recorder.Count) +
                gonePersons.Sum(p => p.sender.Recorder.Count + p.receiver.recorder.Count);

            Logger.Log("Active Recorders: " + string.Join("\n", string.Join("\n", activePersons.Select(p => p.name + ": " + p.sender.Recorder.Count + " sender, " + p.receiver.Recorder.Count + " receivers"))));
            Log("Left Recorders: " + string.Join("\n", string.Join("\n", gonePersons.Select(p => p.name + ": " + p.sender.Recorder.Count + " sender, " + p.receiver.Recorder.Count + " receivers"))));
            Log("Total Recorder Messages: " + sum);
            Log("Messages : " + log.Count + " messages");
            nextMemDump += memDumpInterval;
        }

        if (nextMemoryPrint <= time.time)
        {
            if (_totalReservedMemoryRecorder.Valid)
                Log(string.Format("Total Reserved Memory: {0:n} MB", _totalReservedMemoryRecorder.LastValue / (1024 * 1024)));

            nextMemoryPrint += memoryPrintInterval;
            */

        if (exiting)
        {
            if (Time.fixedTime > waitForExitEnd)
            {
                Logger.LogWarning("Timeout for sending Logs");
                Exit();
                return;
            }

            if (simulationSychronizer.CanQuit)
            {
                Logger.LogWarning("Simulation can be quit");
                Exit();
            }
            return;
        }

        if (wantsToExit)
        {
            exiting = true;
            _ = simulationSychronizer.QuitAsync();
            waitForExitEnd = Time.fixedTime + 20f * (activePersons.Count + 10); // waiting if something is missing and + 2mins for log
        }
        if (leaveIndex > 2)
            CleanUnusedPersons();

        if (syncing)
        {
            if (simulationSychronizer.completed)
            {
                simulationSychronizer.success = true;
                Quit();
            }
            return;
        }

        while (time.time >= nextSpawnAt)
        {
            // Spawn
            var person = persons[spawnIndex];
            person.gameObject.SetActive(true);
            activePersons.Add(person);

            LogState();

            // if end reached, set next spawn at infinite
            spawnIndex++;
            if (spawnIndex >= spawnPersonCount)
                nextSpawnAt = float.PositiveInfinity;
            else
                nextSpawnAt = persons[spawnIndex].spawnAt;
        }

        while (time.time >= nextLeaveAt)
        {
            leaveIndex++;
            if (leaveIndex >= spawnPersonCount)
                nextLeaveAt = float.PositiveInfinity;
            else
                nextLeaveAt = leavePersonTimes[leaveIndex];
        }

        SimulationFixedUpdate();

        if (persons.Length <= spawnIndex && activePersons.Count == 0 && IsSimulationEnd())
            Complete();


        if (simulationEndWithBuffer <= time.time)
        {
            Logger.LogWarning("Simulation Timeout");
            Logger.LogWarning(activePersons.Count + " persons are active");
            foreach (var p in activePersons)
            {
                ActivePersonDump(p);
            }
            Debug.LogError("Simulation Timeout");
            OnError();
        }

    }

    bool syncing = false;
    bool exiting = false;
    float waitForExitEnd; 

    /**
     * Executed if the simulation is completed
     **/
    protected void Complete()
    {
        syncing = true;
        if (simulationSychronizer.SimulationId > 0)
        {
            infoText.text = "Synching...";
        Logger.Log("Synching data...");
        simulationSychronizer.SyncData(time.time);
        autoSpeed.Disable();
        }
        
        Time.timeScale = 1f;
    }

    void OnApplicationQuit()
    {
        Debug.Log("Quit");
        wantsToExit = true;
    }

    public void OnError()
    {
        hasError = true;
        Quit();
    }

    bool wantsToExit = false;
    protected void Quit()
    {
        Debug.Log("Simulation wants to quit.");
        simulationSychronizer.Exit(hasError);
        wantsToExit = true;
    }

    void Exit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    protected virtual void ActivePersonDump(GenericPersonAi p)
    {
        Logger.Log(p.name + ": " + p.transform.position.ToString());
        Logger.Log(GetGameObjectPath(p.gameObject) + JsonUtility.ToJson(p, true));
        var movement = p.GetComponent<PersonMovement>();
        if (movement != null)
        {
            Logger.Log(JsonUtility.ToJson(movement, true));
        }
    }

    protected static string GetGameObjectPath(GameObject obj)
    {
        string path = "/" + obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = "/" + obj.name + path;
        }
        return path;
    }

    protected abstract void SimulationFixedUpdate();

    protected abstract bool IsSimulationEnd();

    protected abstract void Spawn();

    private void LateUpdate()
    {
        if (time.time < uiUpdate) return;

        if (!syncing)
        {
            infoText.text = "Persons: " + (activePersons.Count) + "/" + spawnPersonCount;
            if (nextSpawnAt < nextLeaveAt)
                infoText.text += "\nNext spawn in " + (nextSpawnAt - time.time).ToString("00") + "s";
            else if (!float.IsInfinity(nextLeaveAt))
                infoText.text += "\nNext leave in " + (nextLeaveAt - time.time).ToString("00") + "s";
            else
                infoText.text += "\nEnding simulation in " + System.TimeSpan.FromSeconds((int)simulationEnd - (int)time.time).ToString();
        }

        UiUpdate();

        uiUpdate = time.time + 1f;

    }

    protected virtual void UiUpdate()
    {
        // Method can be overridden
    }

    protected bool allGone = false;

    void CleanupReset()
    {
        personCleanupIndex = 0;
    }

    void CleanUnusedPersons()
    {
        if (gonePersons.Count <= personCleanupIndex)
        {
            CleanupReset();
            allGone = activePersons.Count == 0 && spawnIndex >= spawnPersonCount;
            return;
        }

        var person = gonePersons[personCleanupIndex];

        if (person != null && person.CanBeDeleted())
        {
            gonePersons.RemoveAt(personCleanupIndex);
            Object.Destroy(person.gameObject);
            LogState();
            return;
        }

        personCleanupIndex++;
    }

    void LoadingNames()
    {
        using (StringReader reader = new StringReader(firstNamesAsset.text))
        {
            firstNames = new List<string>();

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                firstNames.Add(line);
            }
        }

        using (StringReader reader = new StringReader(surnameAssets.text))
        {
            surnames = new List<string>();

            string line;
            while ((line = reader.ReadLine()) != null)
            {
                surnames.Add(line);
            }
        }
    }

    protected void SetRandomName(int index, MonoBehaviour obj)
    {
        obj.name = string.Format("{0} {1} {2}",
            index,
            firstNames[rng.NextInt(0, firstNames.Count)],
            surnames[rng.NextInt(0, surnames.Count)]);
    }

    protected void InitBladder<T>(T person, Bladder bladder, RandomNumberGenerator rng) where T : MonoBehaviour, IPerson
    {
        bladder.Init(rng);
    }
}


