
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

public class SenderCsvSynchronizer : GenericCsvSychronizer<BLESender, BLEBroadcast<ulong>, BLEBroadcast<ulong>, SenderCsvSyncJob>
{
    public SenderCsvSynchronizer(ulong simulationId, List<BLESender> objects, int jobCount) : base(simulationId, objects, jobCount)
    {
        filename = "senders";
        action = "send";
    }

    protected override BLERecord<BLEBroadcast<ulong>>[] castStructs(int i, BLERecord<BLEBroadcast<ulong>>[] obj)
    {
        return obj;
    }

    protected override SenderCsvSyncJob createJob(int i)
    {
        return new SenderCsvSyncJob();
    }

    protected override JobHandle ScheduleCsvJob(SenderCsvSyncJob job, int jobIndex)
    {
        return job.Schedule();
    }


}
