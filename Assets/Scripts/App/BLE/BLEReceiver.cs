using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class BLEReceiver : MonoBehaviour, IHasRecorder<BLEReceive<ulong>>
{
    [SerializeField]
    BLEReceive<ulong>[] received;

    [SerializeField]
    List<int> receivedUuids;
    public List<int> shouldReceivedUuids;

    public int deviceId;
    public int uuid;
    public float range;
    public bool ignoreGenerated = false;

    public BLEDeviceType deviceType;

    bool recording = true;
    bool recordingGroups = true;

    public BLERecorder<BLEReceive<ulong>> recorder = new BLERecorder<BLEReceive<ulong>>();

    public BLERecorder<BLEReceive<ulong>> Recorder => recorder;

    public int DeviceId => deviceId;

    public BLEDeviceType DeviceType => deviceType;

    [SerializeField] string globalName;

    Dictionary<int, int> uuidIterationsCounter;

    public Dictionary<int, int> UUIDIerationsCounter => uuidIterationsCounter;

    public string GlobalName { get => globalName; }

    public int globalIndex;
    public int GlobalIndex { get => globalIndex; set => globalIndex = value; }

    public int localIndex;
    public int LocalIndex { get => localIndex; }

    public string LocalName  { get => name; }
    public bool Synced { get; set; }

    public void SetSenderCount(int count) => received = new BLEReceive<ulong>[count];

    SimulationSychronizer simulationSynchronizer;

    void Awake()
    {
        simulationSynchronizer = FindObjectOfType<SimulationSychronizer>();
        range = SimulationSettings.Instance.BroadcastRange;
    }

    private void Start()
    {
        deviceId = gameObject.GetInstanceID();
        simulationSynchronizer.Register(this);
        receivedUuids = new List<int>();
        shouldReceivedUuids = new List<int>();
        if (recordingGroups)
            uuidIterationsCounter = new Dictionary<int, int>();
    }

    void OnDestroy()
    {
        if (simulationSynchronizer != null)
            simulationSynchronizer.Deregister(this);
    }

    public virtual void Receive(BLEBroadcast<ulong> broadcast, float distance, uint continuation)
    {
        var msg = new BLEReceive<ulong>(broadcast, distance, continuation);
        if (recording)
        {
            recorder.Add(transform, msg);
        }

        receivedUuids.Add(broadcast.package.uuid);
        received[broadcast.package.uuid] = msg;
    }

    internal void DeviceInRange(int uuid)
    {
        shouldReceivedUuids.Add(uuid);
    }

    public virtual IEnumerable<BLEReceive<ulong>> GetLastReceivedMessagesBySender()
    {
        var dict = new Dictionary<int, BLEReceive<ulong>>();

        for (int i = 0; i < receivedUuids.Count; i++)
        {
            var uuid = receivedUuids[i];
            dict[uuid] = received[uuid];
        }

        if (recordingGroups)
        { 
            foreach (var e in dict)
            {
                if (uuidIterationsCounter.TryGetValue(e.Key, out var counter))
                    uuidIterationsCounter[e.Key] = counter + 1;
                else
                    uuidIterationsCounter[e.Key] = 1;
            }
        }

        receivedUuids.Clear();
        shouldReceivedUuids.Clear();
        return dict.Values;
        
    }

    public void ResetUuidCounter()
    {
        uuidIterationsCounter.Clear();
    }

    public void DisableRecording()
    {
        recording = false;
        recorder.Flush();
    }

    public void DisableGroupRecording()
    {
        recordingGroups = false;
        uuidIterationsCounter = null;
    }

    public bool IsRecording => recording;    
    public bool IsRecordingGroups => recordingGroups;

}
