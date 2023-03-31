using System;
using UnityEngine;


[Serializable]
public struct RegisterResponse
{
    public ulong id;
}

[Serializable]
public struct RegisterRequest
{
    public string scenario;
    public int seed;
    public string version;
    public string algorithm;
    public string device_name;
    public string mode;
    public string recording;
    public float broadcast_interval;
    public float app_interval;
    public string os;
    public string platform;
    public int person_count;
    public string simulation_options;
    public float receive_accuracy;
}


[Serializable]
public struct EndRequest
{
    public string status;
    public float end_time;
}

[Serializable]
public struct LogRequest
{
    public string log;
}

[Serializable]
public struct AccuracyRequest
{
    public AccuracyEntry[] log;
}


[Serializable]
public struct MessagesRequest<T>
{
    public int device_id;
    public string type;
    public T[] packages;
}

[Serializable]
public struct SendPackage
{
    public ulong id;
    public float time;
    public Vector3 position;
    public bool generated;
    public int uuid;
    public string value;
}

[Serializable]
public struct ReceivedPackage
{
    public ulong id;
    public float time;
    public Vector3 position;
    public double distance;
    public int uuid;
    public string value;
}