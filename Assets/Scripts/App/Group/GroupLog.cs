
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

[Serializable]
public struct GroupLogDevice
{
    public int remote_uuid;
    public int iterations;
}

[Serializable]
public struct GroupLogMember
{
    public int uuid;
    public GroupLogDevice[] loggedDevices;
}

[Serializable]
public struct GroupLog
{
    public float time;
    public int t;
    public ulong gid;
    public int devices;
    public GroupLogDevice[] received;
}


[Serializable]
public struct GroupLogCsvEntry
{
    public float time;
    public int t;
    public ulong gid;
    public int devices;
    public int receivedUuids;
}