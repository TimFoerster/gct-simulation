using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RandomNumberGenerator))]
public partial class BLESender : MonoBehaviour, IHasRecorder<BLEBroadcast<ulong>>
{
    public int deviceId;
    public int uuid;
    Cid<ulong> cid;
    public Vector2 recentPosition;
    BLEReceiver receiver;
    GenericPersonAi person;
    //List<Receiver> staticReceivers;
    
    // Recorder
    [SerializeField] string globalName;
    public string GlobalName { get => globalName; }

    public int globalIndex;
    public int GlobalIndex { get => globalIndex; set => globalIndex = value; }

    public int localIndex;
    public int LocalIndex { get => localIndex; }

    public string LocalName { get => name; }

    [SerializeField]
    bool recording = true;
    BLERecorder<BLEBroadcast<ulong>> recorder = new BLERecorder<BLEBroadcast<ulong>>();
    public BLERecorder<BLEBroadcast<ulong>> Recorder => recorder;

    public BLEDeviceType deviceType = BLEDeviceType.Device;

    public int DeviceId => deviceId;

    public BLEDeviceType DeviceType => deviceType;

    public bool Synced { get; set; }

    public int Timeslot;

    public Cid<ulong> Cid { get => cid; set => cid = value; }
    SimulationSychronizer simulationSynchronizer;

    [SerializeField]
    SimulationTime time;

    List<int> recentBroadcastedUuids = new List<int>();
    List<int> sendAtUuuid = new List<int>();

    uint[] numberOfSendContinuesMessages;

    public void SetReceiverCount(int count) => numberOfSendContinuesMessages = new uint[count];
    float receiveAccuracy;
    RandomNumberGenerator rng;

    private void Awake()
    {
        receiver = GetComponent<BLEReceiver>();
        person = GetComponentInParent<GenericPersonAi>();
        simulationSynchronizer = FindObjectOfType<SimulationSychronizer>();
        time = FindObjectOfType<SimulationTime>();
        receiveAccuracy = SimulationSettings.Instance.ReceiveAccuarcy;
        rng = GetComponent<RandomNumberGenerator>();
    }


    //System.Random rng;

    public void OnEnable()
    {
        var seed = Random.Range(int.MinValue, int.MaxValue);
        //rng = new System.Random(seed);
        // Logger.LogSimulation(GlobalIndex + " rng receive seed: " + seed, "rng");
    }

    // Start is called before the first frame update
    void Start()
    {
        receiver.uuid = uuid = person.personIndex;
        deviceId = gameObject.GetInstanceID();
        simulationSynchronizer.Register(this);
    }

    public void DisableRecording()
    { 
        recording = false;
    }

    public bool IsRecording => recording;

    void OnDestroy()
    {
        if (simulationSynchronizer != null)
            simulationSynchronizer.Deregister(this);
    }


    public void Broadcast(IEnumerable<Receiver> receivers, bool staticEnv = false)
    {
        var package = new BLEAdvPackage<ulong>(uuid, cid.id, time.time);
        var broadcast = new BLEBroadcast<ulong>(package, cid.generated);
        Broadcast(broadcast, receivers);
    }

    bool IsReceivingMessage()
    {
        var v = rng.Range();
        //Logger.LogSimulation(globalIndex + " ~ " + v, "rng");
        return v <= receiveAccuracy;
    }

    public void Broadcast(BLEBroadcast<ulong> broadcast, IEnumerable<Receiver> receivers)
    {
        if (recording)
            recorder.Add(transform, broadcast);

        sendAtUuuid.Clear();

        foreach (var receiver in receivers)
        {
            if (receiver.receiver.ignoreGenerated) // BLE beacon
            {
                if (cid.generated)
                    continue;

            } else // Device
            {
                if (!IsReceivingMessage())
                {
                    // Logger.LogSimulation(globalIndex + " -> " + receiver.receiver.GlobalIndex + " dropped", "sender");
                    continue;
                }
            }

            receiver.receiver.Receive(broadcast, receiver.magnitude, numberOfSendContinuesMessages[receiver.receiver.uuid]);
            sendAtUuuid.Add(receiver.receiver.uuid);
            recentBroadcastedUuids.Remove(receiver.receiver.uuid);
            numberOfSendContinuesMessages[receiver.receiver.uuid]++;
        }

        
        foreach (var rbuuid in recentBroadcastedUuids)
        {
            numberOfSendContinuesMessages[rbuuid] = 0;
        }

        recentBroadcastedUuids = new List<int>(sendAtUuuid.ToArray());
    }


}

