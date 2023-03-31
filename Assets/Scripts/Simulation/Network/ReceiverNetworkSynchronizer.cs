
using System;
using System.Collections.Generic;
using Unity.Jobs;
using UnityEngine;

internal class ReceiverNetworkSynchronizer : JobSychronizerOrganisator<BLEReceiver, BLEReceive<ulong>, BLEReceive<ulong>, ReceiverNetworkSyncJob>
{

    public ReceiverNetworkSynchronizer(ulong simulationId, List<BLEReceiver> receivers, int jobs = 2) : base(simulationId, receivers, jobs)
    {
    }


    protected override ReceiverNetworkSyncJob createJob(int i)
    {
        return new ReceiverNetworkSyncJob();
    }

    protected override JobHandle ScheduleJob(ReceiverNetworkSyncJob job, BLEReceiver recorderObject, int jobIndex)
    {

        return job.Schedule();
    }

    protected override void CompleteJob(JobHandle job, int jobIndex)
    {
        job.Complete();
    }

    protected override BLERecord<BLEReceive<ulong>>[] castStructs(int i, BLERecord<BLEReceive<ulong>>[] obj)
    {
        return obj;
    }
}