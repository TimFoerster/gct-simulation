
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

internal class SenderNetworkSychronizer : JobSychronizerOrganisator<BLESender, BLEBroadcast<ulong>, BLEBroadcast<ulong>, SenderNetworkSyncJob>
{
    public SenderNetworkSychronizer(ulong simulationId, List<BLESender> senders) : base(simulationId, senders)
    {
    }

    protected override BLERecord<BLEBroadcast<ulong>>[] castStructs(int i, BLERecord<BLEBroadcast<ulong>>[] obj)
    {
        return obj;
    }

    protected override void CompleteJob(JobHandle jobHandle, int jobIndex)
    {
        jobHandle.Complete();
    }

    protected override SenderNetworkSyncJob createJob(int i)
    {
        return new SenderNetworkSyncJob();
    }

    protected override JobHandle ScheduleJob(SenderNetworkSyncJob job, BLESender sender, int jobIndex)
    {
        return job.Schedule();
    }

}
