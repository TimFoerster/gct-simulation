
using System;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

internal class ReceiverCsvSynchronizer : GenericCsvSychronizer<BLEReceiver, BLEReceive<ulong>, BLEReceive<ulong>, ReceiverCsvSyncJob>
{
    public ReceiverCsvSynchronizer(ulong simulationId, List<BLEReceiver> objects, int jobCount) : base(simulationId, objects, jobCount)
    {
        filename = "receivers";
        action = "received";
    }

    protected override BLERecord<BLEReceive<ulong>>[] castStructs(int i, BLERecord<BLEReceive<ulong>>[] obj)
    {
        return obj;
    }

    protected override ReceiverCsvSyncJob createJob(int i)
    {
        return new ReceiverCsvSyncJob();
    }

    protected override JobHandle ScheduleCsvJob(ReceiverCsvSyncJob job, int jobIndex)
    {
        return job.Schedule();
    }
    
}
