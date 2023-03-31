using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(RandomNumberGenerator))]
public class TracingApp : MonoBehaviour, IHasRecorder<GroupLog>
{
    public IAlgorithm<Cid<ulong>> algorithm;

    public Cid<ulong> cid;

    public BLESender sender;
    public BLEReceiver receiver;
    public bool truePositive;

    [SerializeField]
    SpriteRenderer body;

    [SerializeField]
    GenericPersonAi person;

    public GenericPersonAi Person => person;

    [SerializeField]
    RandomNumberGenerator rng;
    public int Timeslot;
    AppHandler appHandler;

    SimulationSettings settings;

    public int t;

    [SerializeField]
    public List<GroupLog> groupLog;

    SimulationTime time;

    public int Uuid => sender.uuid;

    public List<int> shouldReceivedUuids => receiver.shouldReceivedUuids;

    public bool recordingGroups = true;

    public void Init(AppHandler appHandler, SimulationTime time)
    {
        this.appHandler = appHandler;
        this.time = time;
        Recorder = new BLERecorder<GroupLog>();
    }

    SimulationSychronizer simulationSynchronizer;

    public void Awake()
    {
        simulationSynchronizer = FindObjectOfType<SimulationSychronizer>();
        truePositive = true;
    }

    public void Start()
    {
        if (appHandler != null)
            appHandler.AddApp(this);

        t = 0;
        groupLog = new List<GroupLog>();
        simulationSynchronizer.Register(this);
    }

    void OnDestroy()
    {
        if (simulationSynchronizer != null)
            simulationSynchronizer.Deregister(this);
    }

    public int[] OnAppTrigger()
    {
        var x = algorithm.CalculateNextCid(sender.Cid, receiver);
        if (recordingGroups && receiver.IsRecordingGroups)
        {
            if (x.Item1.generated)
            {
                receiver.ResetUuidCounter();
            } else if (x.Item1.id == sender.Cid.id)
            {

                Recorder.Add(transform, new GroupLog
                {
                    devices = x.Item2.Length,
                    gid = x.Item1.id,
                    t = t,
                    time = time.time,
                    received = receiver.UUIDIerationsCounter.Select(entry =>
                        new GroupLogDevice
                        {
                            remote_uuid = entry.Key,
                            iterations = entry.Value
                        }
                    ).OrderBy(e => e.remote_uuid).ToArray()
                });

                // Logger.LogSimulation(receiver.GlobalIndex.ToString() + ": New Group entry: " + x.Item1.id.ToString() + " with " + x.Item2.Length + " other devices", "app");

                receiver.ResetUuidCounter();
            }
        }
        UpdateCid(x.Item1);
        t++;
        return x.Item2;
    }

    private void OnEnable()
    {
        rng.enabled = true;

        settings = SimulationSettings.Instance;
        algorithm = settings.AlgorithmFactory.CreateAlgorithm(rng);
        UpdateCid(algorithm.Init());
    }



    public void UpdateCid(Cid<ulong> newCid)
    {
        cid = newCid;
        sender.Cid = newCid;

        if (!person.IsVisible()) return;

        UpdateColor(newCid);
    }

    internal void OverlayTypeChanged(OverlayType overlayType)
    {
        if (overlayType == OverlayType.None)
        {
            body.color = Color.gray;
            return;
        }
    }

    internal void OnEnterExit()
    {
        appHandler.RemoveApp(this);
    }

    void UpdateColor(Cid<ulong> newCid)
    {
        switch (OverlayType){
            case OverlayType.None: return;
            case OverlayType.Statistics:
                body.color = Utils.U64ToHSV(newCid.id);
                return;
            case OverlayType.Groups:
                body.color = appHandler.GetGroupColor(this);
                return;

        }
    }

    public void DisableRecording()
    {
        recordingGroups = false;
    }

    OverlayType OverlayType => appHandler == null ? OverlayType.None : appHandler.OverlayType;


    public BLERecorder<GroupLog> Recorder { get; set; }

    public int DeviceId => sender.DeviceId;

    public BLEDeviceType DeviceType => sender.DeviceType;

    public string GlobalName => sender.GlobalName;

    public int GlobalIndex => sender.GlobalIndex;

    public int LocalIndex => sender.localIndex;

    public string LocalName => sender.LocalName;

    public bool Synced { get; set ; }

    public bool IsRecording { get => recordingGroups; set => recordingGroups = value; }
}
