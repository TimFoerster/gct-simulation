using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;


[Serializable]
public struct AccuracyEntry
{
    public uint timestep;
    public float time;
    public double accuracy;
    public int person_count;
}


[Serializable]
struct AppGroup
{
    public int id;
    public Color color;
    public List<int> apps;
    public ulong gid;
    public ulong mean;
    public ulong margin;
    public ulong max_diff;
}



[Serializable]
struct GroupLogEntry
{
    public int id;
    public float time;
    public ulong gid;
    public ulong mean;
    public int appsCount;
    public ulong max_diff;
    public GroupLogMember[] members;
}

[RequireComponent(typeof(RandomNumberGenerator))]
public class AppGroupManager: MonoBehaviour
{
    [SerializeReference]
    TracingApp[] apps;

    public TracingApp[] Apps => apps;

    // app id to group
    [SerializeField]
    int[] appToGroup;
    [SerializeField]
    int[] appInGroupIntervalCounts;

    int intervalInGroup;
    ulong defaultMargin;

    [SerializeField]
    AppGroup[] groups;

    RandomNumberGenerator rng;
    int[][] receivedUuids;

    const int colorCount = 255;

    [SerializeField]
    Color[] colors;

    Color defaultColor = new Color(.5f,.5f,.5f,.5f);

    [SerializeField]
    SimulationTime time;

    [SerializeField]
    List<int> activeGroups;

    bool recordAccuracy = true;
    bool recordGroups = true;

    void Awake()
    {
        rng = GetComponent<RandomNumberGenerator>();
        var settings = SimulationSettings.Instance;
        intervalInGroup = settings.AppGroupInterval;
        defaultMargin = settings.AppGroupMargin;
        recordAccuracy = settings.RecordAccuracy;
        recordGroups = settings.RecordGroups;

        colors = new Color[colorCount];
        for (int i = 0; i < colorCount; i++)
        {
            colors[i] = Color.HSVToRGB(
                rng.Range(0f, 1f),
                rng.Range(.2f, 1f),
                rng.Range(.2f, 1f)
            );
        }
    }

    public void SetDeviceCount(int deviceCount)
    {
        groups = new AppGroup[deviceCount];
        apps = new TracingApp[deviceCount];
        appInGroupIntervalCounts = new int[deviceCount];
        appToGroup = new int[deviceCount];
        for (int i = 0; i < deviceCount; i++)
        {
            appToGroup[i] = -1;
        }

        receivedUuids = new int[deviceCount][];
    }

    public void Add(TracingApp app)
    {
        apps[app.Uuid] = app;
        activeApps.Add(app);
    }

    public void Remove(TracingApp app)
    {
        var gId = appToGroup[app.Uuid];
        if (gId > -1)
        {
            groups[gId].apps.Remove(gId);
            appToGroup[app.Uuid] = -1;
        }
        apps[app.Uuid] = null;
        activeApps.Remove(app);
    }

    internal Color GetGroupColor(TracingApp app)
    {
        if (appToGroup == default) 
            return defaultColor;
        int id = appToGroup[app.Uuid];
        if (id == -1 || appInGroupIntervalCounts[app.Uuid] < intervalInGroup) 
            return defaultColor;

        return groups[id].color;
    }

    List<int> groupChanges = new List<int>();
    List<int> loggedGroups = new List<int>();



    internal void Updated(TimeslotEntries<TracingApp> tracingApps)
    {
        AppGroup group;

        foreach (TracingApp app in tracingApps)
        {
            var uuids = receivedUuids[app.Uuid];
            var groupId = appToGroup[app.Uuid];
            
            if (uuids.Length == 0) // Noone in Range? lets leave group
            {
                if (groupId > -1)
                {
                    LeaveGroup(groupId, app);
                }

                continue;
            }

            // check if our group is fine
            if (groupId > -1)
            {
                group = groups[groupId];

                // are we still near the group?
                ulong groupDiff1 = group.mean - app.cid.id;
                ulong groupDiff2 = app.cid.id - group.mean;

                // if group empty or our mean value is to far away, leave group
                if (group.apps.Count < 2 || (groupDiff1 > group.margin && groupDiff2 > group.margin))
                {
                    LeaveGroup(groupId, app);
                    continue;
                }

                appInGroupIntervalCounts[app.Uuid]++;
            }

            // check if we need to merge or create groups
            foreach (var uuid in uuids) 
            {
                var otherGroupId = appToGroup[uuid];
                if (apps[uuid] == null)// user gone
                    continue;

                // if we both have no group, check if can create a new group
                if (groupId == -1 && otherGroupId == -1)
                {
                    var diff1 = app.cid.id - apps[uuid].cid.id;
                    var diff2 = apps[uuid].cid.id - app.cid.id;

                    if (diff1 <= defaultMargin || diff2 <= defaultMargin)
                    {
                        // groupid should always be the lower one, with equal size
                        var gId = app.Uuid < uuid ? app.Uuid : uuid;
                        groups[gId] = new AppGroup
                        {
                            id = gId,
                            apps = new List<int>() { app.Uuid, uuid },
                            color = colors[gId % colorCount],
                            margin = defaultMargin
                        };
                        appToGroup[app.Uuid] = gId;
                        appToGroup[uuid] = gId;
                        appInGroupIntervalCounts[app.Uuid] = 0;
                        appInGroupIntervalCounts[uuid] = 0;
                        UpdateGroup(ref groups[gId]);
                        groupChanges.Add(gId);
                        activeGroups.Add(gId);

                        break;
                    }
                    continue;
                }

                // we dont care about other empty person groups
                if (otherGroupId == -1) continue;

                // if we are in the same group, nothing needs to be merged
                if (otherGroupId == groupId && groupId != -1) continue;

                var otherGroup = groups[otherGroupId];

                // we are in different groups, look if we can join them
                if (groupId != -1) // we have a group
                {
                    group = groups[groupId];
                    // we are in the larger group
                    if (group.apps.Count > otherGroup.apps.Count) continue; 

                    // If the group has the same app count, let the bigger groupId join the lower one
                    if (group.apps.Count == otherGroup.apps.Count && groupId < otherGroupId) continue; 
                }

                var diffA = app.cid.id - otherGroup.mean;
                var diffB = otherGroup.mean - app.cid.id;

                if (diffA <= otherGroup.margin || diffB <= otherGroup.margin)
                {
                    // leave current group
                    if (groupId != -1)
                    {
                        LeaveGroup(groupId, app);
                    }

                    // join other
                    appToGroup[app.Uuid] = otherGroupId;
                    otherGroup.apps.Add(app.Uuid);
                    UpdateGroup(ref otherGroup);
                    appInGroupIntervalCounts[app.Uuid] = 0;
                    groupChanges.Add(otherGroupId);
                    break;
                }

            }
        }

        foreach(var gId in activeGroups.ToArray()) 
        {
            UpdateGroup(ref groups[gId]);
        }

        /*
        foreach (var groupId in groupChanges)
        {
            LogGroup(groupId);
        }*/

        groupChanges.Clear();
    }

    void LeaveGroup(int groupId, TracingApp app)
    {
        var group = groups[groupId];
        if (groupId == app.Uuid)
        {
            foreach (var groupMember in group.apps.ToArray())
            {
                appToGroup[groupMember] = -1;
                group.apps.Remove(app.Uuid);
            }
        } else
        {
            appToGroup[app.Uuid] = -1;
            group.apps.Remove(app.Uuid);
        }

        groupChanges.Add(groupId);
        UpdateGroup(ref group);
    }

    void UpdateGroup(ref AppGroup group)
    {
        var ids = group.apps.Where(a => apps[a] != null).Select(a => apps[a].cid.id).ToArray();
        ulong v = 0;
        bool eq = ids.Length > 0;
        for(int i = 0; i < ids.Length; i++)
        {
            if (i == 0)
            {
                v = ids[0];
                continue;
            }

            if ( v != ids[i])
            {
                eq = false;
                break;
            }
        }

        if (eq)
        {
            group.mean = v; 
            group.max_diff = 0;
            return;
        }

        var mean = CidCalculator.meanBySumVector(ids);
        group.mean = mean;
        if (ids.Length > 0)
        {
            group.gid = ids.First();
            group.max_diff = ids.Max(id =>
            {
                var diffA = id - mean;
                var diffB = mean - id;
                return diffA < diffB ? diffA : diffB;
            });
        } else
        {
            activeGroups.Remove(group.id);
            return;
        }
    }

    [SerializeField]
    List<GroupLogEntry> groupLog = new List<GroupLogEntry>();

    internal List<GroupLogEntry> ItemsToSync()
    {
        return new List<GroupLogEntry>(groupLog);
    }

    internal void CompletedSync(int count)
    {
        groupLog.RemoveRange(0, count);
    }

    internal void DisableRecording()
    {
        recordAccuracy = false;
        recordGroups = false;
        groupLog = null;
        accuracyLog = null;
    }

    internal void LogGroup(int gid)
    {
        var group = groups[gid];
        if (recordGroups)
            groupLog.Add(
                new GroupLogEntry
                {
                    id = gid,
                    appsCount = group.apps.Count,
                    gid = group.gid,
                    mean = group.mean,
                    time = time.time,
                    max_diff = group.max_diff,
                    members = group.apps.Where(uuid => apps[uuid] != null).Select(uuid => 
                        new GroupLogMember // UUIDIteration might be already emptied by Tracing App => analyse this later in DB.
                        {
                            uuid = uuid,
                            loggedDevices = apps[uuid].receiver.UUIDIerationsCounter.Select(r => 
                                new GroupLogDevice
                                {
                                    remote_uuid = r.Key,
                                    iterations = r.Value
                                }
                            ).ToArray()
                        }).ToArray()
                }
            );

    }

    internal void ReceivedIds(TracingApp app, int[] ids) =>
        receivedUuids[app.Uuid] = ids;


    [SerializeField]
    protected List<AccuracyEntry> accuracyLog;

    double accuracy = -1;
    List<TracingApp> activeApps = new List<TracingApp>();
    int sendIndex = 0;

    internal void UpdateAccuracy(TimeslotEntries<TracingApp> tracingApps)
    {
        double prefAccuracy = accuracy;
        if (activeApps.Count == 0)
        {
            accuracy = -1;
        }
        else { 
            foreach (TracingApp app in tracingApps)
            {
                var uuid = app.Uuid;

                var uuids = receivedUuids[app.Uuid];
                if (uuids.Length == 0 && app.shouldReceivedUuids.Count == 0)
                {
                    app.truePositive = true;
                    continue;
                }

                var v = app.sender.Cid.id;
                var status = true;
                foreach (int rid in uuids) // if one has not the same gid => false
                {
                    if (apps[rid] != null && v != apps[rid].cid.id)
                    {
                        status = false;
                        break;
                    }
                }
                if (status)
                {
                    foreach (int rid in app.shouldReceivedUuids) // if one has not the same gid => false
                    {
                        if (apps[rid] != null && v != apps[rid].cid.id)
                        {
                            status = false;
                            break;
                        }
                    }
                }

                app.truePositive = status;
            }

            accuracy = activeApps.Where(a => a.truePositive == true).Count() / (double) activeApps.Count();
        }

        if (accuracy == prefAccuracy)
            return;

        if (recordAccuracy)
            accuracyLog.Add(new AccuracyEntry
            {
                timestep = time.Counter,
                time = time.time,
                accuracy = accuracy >= 0 ? accuracy : double.NaN,
                person_count = activeApps.Count()
            });
    }

    ulong simId;
    public async void SetSimulationId(ulong id)
    {
        simId = id;
        filePath = Path.Combine(Application.dataPath, "../Data", simId.ToString(), filename);
        await CreateCsvFile();
    }

    public async Task<bool> SyncAccuracyAsync(SimulationSychronizer simulationSychronizer)
    {

        var zipArchive = filename + ".zip";
        using (FileStream fs = new FileStream(zipArchive, FileMode.Create))
        {
            using ZipArchive arch = new ZipArchive(fs, ZipArchiveMode.Create);
            arch.CreateEntryFromFile(filePath, filename);

        };

        var okay = SimulationServerCommunication.sendFile(
            simId + "/accuracy/file",
            await File.ReadAllBytesAsync(zipArchive),
            zipArchive,
            simId
        );

        if (okay)
        {
            File.Delete(filePath);
            File.Delete(zipArchive);
            completed = true;
        }

        return okay;
    }

    string filename = "accuracy.csv";
    string filePath;
    public bool completed = false;

    public async Task WriteCsvFile()
    {
        if (sendIndex > 0 || accuracyLog == null || accuracyLog.Count == 0 || simId == default) return;

        using (var writer = new CsvStreamWriter(filePath, true))
        {
            sendIndex = accuracyLog.Count;

            await writer.WriteLinesAsync(
                accuracyLog.Select(record =>
                    string.Format(
                        writer.FormatProvider,
                        "{0},{1},{2},{3}",
                        record.timestep,
                        record.time,
                        record.accuracy,
                        record.person_count
                    )
                )
            );

            accuracyLog.RemoveRange(0, sendIndex);
            sendIndex = 0;
        }
    }

    public async Task CreateCsvFile()
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "../Data", simId.ToString()));

        using var writer = new CsvStreamWriter(filePath);
        var fields = new string[] { "timestep", "time", "accuracy", "person_count" };
        await writer.WriteHeaderAsync(fields);
    }

    const string display = "{0:000.000} % ACC";
    [SerializeField] private TextMeshProUGUI accuracyText;

    private void LateUpdate()
    {
        accuracyText.text = string.Format(display, accuracy * 100);
    }
}
