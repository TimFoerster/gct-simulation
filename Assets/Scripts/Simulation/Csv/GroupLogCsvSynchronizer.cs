
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

internal class GroupLogCsvSynchronizer : GenericCsvSychronizer<TracingApp, GroupLog, GroupLogCsvEntry,GroupLogSyncJob>
{
    NativeArray<GroupLogDevice>[] devicesArray;

    public GroupLogCsvSynchronizer(ulong simulationId, List<TracingApp> objects, int jobCount) : base(simulationId, objects, jobCount)
    {
        filename = "app";
        action = "group";
        devicesArray = new NativeArray<GroupLogDevice>[jobCount];
    }

    protected override BLERecord<GroupLogCsvEntry>[] castStructs(int jobIndex, BLERecord<GroupLog>[] obj)
    {
        List<GroupLogDevice> devices = new();

        var obs =  obj.Select(o => {
            devices.AddRange(o.message.received);
            return new BLERecord<GroupLogCsvEntry>
            {
                index = o.index,
                position = o.position,
                message = new GroupLogCsvEntry
                {
                    devices = o.message.devices,
                    gid = o.message.gid,
                    t = o.message.t,
                    time = o.message.time,
                    receivedUuids = o.message.received.Length
                }
            };
        }).ToArray();

        devicesArray[jobIndex] = new NativeArray<GroupLogDevice>(devices.ToArray(), Allocator.Persistent);

        return obs;
    }

    protected override GroupLogSyncJob createJob(int i)
    {
        return new GroupLogSyncJob();
    }

    protected override JobHandle ScheduleCsvJob(GroupLogSyncJob job, int jobIndex)
    {
        job.devices = devicesArray[jobIndex];
        return job.Schedule();
    }

    protected override void CompleteJob(JobHandle job, int jobIndex)
    {
        base.CompleteJob(job, jobIndex);
        if (devicesArray[jobIndex] != default)
        {
            devicesArray[jobIndex].Dispose();
            devicesArray[jobIndex] = default;
        }
    }
}
