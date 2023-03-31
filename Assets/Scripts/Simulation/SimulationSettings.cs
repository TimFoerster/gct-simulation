using System;
using UnityEngine;
using UnityEngine.Rendering;


public sealed class SimulationSettings
{
    private static SimulationSettings instance = null;

    private string algoName;
    private IAlgorithmFactory<Cid<ulong>> algorithmFactory;
    private float appUpdateInterval;
    private float broadcastInterval;
    private int seed;
    private bool recordAppSenders = true;          // S
    private bool recordAppReceivers = true;        // R
    private bool recordBeaconReceivers = true;     // B
    private bool recordGroups = true;              // G
    private bool recordAccuracy = true;            // A
    private bool groupOverlay;
    private float broadcastRange = 3f;
    private ulong appGroupMargin = 0;
    private int appGroupInterval = 5;
    private bool offlineMode = false;
    private float receiveAccuracy = 0.85f;

    public string AlgorithmName => algoName;
    public IAlgorithmFactory<Cid<ulong>> AlgorithmFactory => algorithmFactory;
    public float AppUpdateInterval => appUpdateInterval;
    public float BroadcastInterval => broadcastInterval;
    public bool GroupOverlay => groupOverlay;
    public float BroadcastRange => broadcastRange;
    public ulong AppGroupMargin => appGroupMargin;
    public int AppGroupInterval => appGroupInterval;
    public bool OfflineMode => offlineMode;
    public float ReceiveAccuarcy => receiveAccuracy;

    public bool RecordAppSenders { get => recordAppSenders; set => recordAppSenders = value; }
    public bool RecordAppReceivers { get => recordAppReceivers; set => recordAppReceivers = value; }
    public bool RecordBeaconReceivers { get => recordBeaconReceivers; set => recordBeaconReceivers = value; }
    public bool RecordGroups { get => recordGroups; set => recordGroups = value; }
    public bool RecordAccuracy { get => recordAccuracy; set => recordAccuracy = value; }

    public int Seed => seed;
    public string Mode;
    public bool IsHeadless;

    public int simulationIndex;

    private SimulationSettings()
    {
        ParseCliArgs();

        IsHeadless = SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null;
        Mode = IsHeadless ? "headless" : "render";

        UpdateRecordingString();
    }

    public readonly string[] algorithms = new string[] {
        "cirucular-mean-ulong-0",
        "cirucular-mean-ulong-4",
        "cirucular-mean-ulong-8",
        "cirucular-mean-ulong-16",
        "cirucular-mean-ulong-32",
    };

    public void SetSeed(int seed) => this.seed = seed;

    private void ParseCliArgs()
    {
        var args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "-algorithm":
                case "-A":
                    SetAlgorithm(args[i+1]);
                    i++;
                    break;

                case "-appupdateinterval":
                case "-AUI":
                    SetAppUpdateInterval(args[i+1]);
                    i++;
                    break;

                case "-broadcastinterval":
                case "-BI":
                    SetBroadcastInterval(args[i+1]);
                    i++;
                    break;

                case "-broadcastrange":
                case "-BR":
                    setBroadcastRange(args[i + 1]);
                    i++;
                    break;

                case "-seed":
                case "-S":
                    SetSeed(args[i+1]);
                    i++;
                    break;

                case "-simulation":
                case "-SIM":
                    SetSimulation(int.Parse(args[i + 1]));
                    i++;
                    break;

                case "-recorder":
                case "-R":
                    SetRecorderOptions(args[i + 1]);
                    i++;
                    break;

                case "-accuracy":
                case "-C":
                    SetReceiveAccuracy(args[i + 1]);
                    i++; 
                    break;

                case "-offline":
                case "-O":
                    offlineMode = true;
                    break;

            }
        }

        SetDefaults();
    }


    private void setBroadcastRange(string v) => 
        broadcastRange = float.Parse(v);

    void SetDefaults()
    {
        if (algoName == null)
            SetAlgorithm();

        if (appUpdateInterval == default)
            appUpdateInterval = 2f;


        if (broadcastInterval == default)
            broadcastInterval = 0.3f;
    }

    string recordingString = "";

    void SetRecorderOptions(string input)
    {
        input = input.ToUpper();

        recordAccuracy = false;
        recordBeaconReceivers = false;
        recordGroups = false;
        recordAppReceivers = false;
        recordAppSenders = false;
        foreach (char c in input)
        {
            switch (c) 
            {
                case 'A':
                    recordAccuracy = true;
                    break;
                case 'B':
                    recordBeaconReceivers = true;
                    break;
                case 'G':
                    recordGroups = true;
                    break;
                case 'R':
                    recordAppReceivers = true;
                    break;
                case 'S':
                    recordAppSenders = true;
                    break;
                default:
                    throw new Exception("Invalid Recorder Option " + c);
            }
        }

        UpdateRecordingString();
    }

    public void UpdateRecordingString()
    {
        recordingString = "";

        if (recordAccuracy)
            recordingString += "A";
        if (recordBeaconReceivers)
            recordingString += "B";
        if (recordGroups)
            recordingString += "G";
        if (recordAppReceivers)
            recordingString += "R";
        if (recordAppSenders)
            recordingString += "S";
    }

    public string RecordingString => recordingString;

    public void SetReceiveAccuracy(string v)
    {
        receiveAccuracy = float.Parse(v);
    }    
    
    public void SetReceiveAccuracy(float v)
    {
        receiveAccuracy = v;
    }


    public void SetAlgorithm(int index)
    {
        SetAlgorithm(algorithms[index]);
    }

    void SetAlgorithm(string name = "cirucular-mean-ulong-0")
    {
        algoName = name;
        algorithmFactory = algoName switch
        {
            "cirucular-mean-ulong-32" => new CircularMeanFactory(true, 32d),
            "cirucular-mean-ulong-16" => new CircularMeanFactory(true, 16d),
            "cirucular-mean-ulong-8" => new CircularMeanFactory(true, 8d),
            "cirucular-mean-ulong-4" => new CircularMeanFactory(true, 4d),
            "cirucular-mean-ulong-0" => new CircularMeanFactory(false),
            _ => throw new Exception("Invalid Algorithm Argument"),
        };
    }

    void SetAppUpdateInterval(string interval) =>
        appUpdateInterval = float.Parse(interval);

    void SetBroadcastInterval(string interval) => 
        broadcastInterval = float.Parse(interval);

    void SetSeed(string seed)
    {
        this.seed = int.Parse(seed);
    }

    void SetSimulation(int index)
    {
        simulationIndex = index;
    }

    public void SetAppUpdateInterval(float interval)
    {
        appUpdateInterval = interval;
    }

    public void SetBroadcastInterval(float interval)
    {
        broadcastInterval = interval;
    }
    

    public static SimulationSettings Instance
    {
        get
        {
            if (instance == null)
            {
                instance = new SimulationSettings();
            }
            return instance;
        }
    }

    
    public void SetGroupOverlay(bool value)
    {
        groupOverlay = value;
    }

    public void SetRecordMode(string value)
    {
        SetRecorderOptions(value);
    }
}
