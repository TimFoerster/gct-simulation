using System.Collections.Generic;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;


public abstract class JobSychronizerOrganisator<O, T, TJ, J> 
        where O : MonoBehaviour, IHasRecorder<T>
        where J : ISyncJob<TJ>
{
    readonly int jobsCount;
    public bool completed = true;
    protected ulong simulationId;
    int loopIndex = 0;

    protected J[] jobs;
    JobHandle[] jobHandlers;
    NativeArray<BLERecord<TJ>>[] messages;
    NativeArray<int>[] statuses;
    public List<O> recorderObjects;

    O[] recorderRefs;

    bool[] hasMessage;

    public JobSychronizerOrganisator(ulong simulationId, List<O> objects, int jobsCount = 2)
    {
        this.jobsCount = jobsCount;
        this.simulationId = simulationId;
        this.recorderObjects = objects;
    }

    public void Destruct()
    {
        for (int i = 0; i < jobsCount; i++)
        {
            CompleteJob(jobHandlers[i], i);

            if (messages[i] != default)
            {
                messages[i].Dispose();
                messages[i] = default;
            }

            if (statuses[i] != default)
            {
                statuses[i].Dispose();
                statuses[i] = default;
            }

        }
    }

    protected abstract BLERecord<TJ>[] castStructs(int i, BLERecord<T>[] obj);
    protected abstract J createJob(int i);

    public virtual void Init()
    {
        jobHandlers = new JobHandle[jobsCount];
        statuses = new NativeArray<int>[jobsCount];
        messages = new NativeArray<BLERecord<TJ>>[jobsCount];
        jobs = new J[jobsCount];
        recorderRefs = new O[jobsCount];
        hasMessage = new bool[jobsCount];

        for (int i = 0; i < jobsCount; i++)
        {
            jobs[i] = createJob(i);
            jobs[i].SimulationId = simulationId;
        }
        
    }

    public void NewLoop()
    {
        completed = recorderObjects == null || recorderObjects.Count == 0;
        loopIndex = 0;
    }


    protected abstract void CompleteJob(JobHandle jobHandle, int jobIndex);
    protected abstract JobHandle ScheduleJob(J job, O recorderObject, int jobIndex);
    protected int SynRecorderObject(O recorderObject, bool final = false)
    {
        var foundProcessor = false;
        var error = false;
        // find sender which is ready to process data 
        for (int jobIndex = 0; jobIndex < jobsCount; jobIndex++)
        {
            if (!jobHandlers[jobIndex].IsCompleted)
                continue;

            if (statuses[jobIndex].Length > 0)
                error = HandleJobCompletion(jobIndex);

            recorderRefs[jobIndex] = recorderObject;

            var job = jobs[jobIndex];

            job.DeviceId = recorderObject.DeviceId;
            job.DeviceType = recorderObject.DeviceType;
            job.Final = !recorderObject.isActiveAndEnabled || final;

            if (hasMessage[jobIndex])
            {
                //Debug.Log(typeof(J).Name + " (" + jobIndex + ") Messages dispose");
                messages[jobIndex].Dispose();
            }

            messages[jobIndex] = new NativeArray<BLERecord<TJ>>(
                castStructs(jobIndex, recorderObject.Recorder.ToSync()), Allocator.Persistent);
            job.Messages = messages[jobIndex];
            hasMessage[jobIndex] = true;

            if (statuses[jobIndex].Length > 0)
            {
                //Debug.Log(typeof(J).Name + " (" + jobIndex + ") Statuses dispose");
                statuses[jobIndex].Dispose();
            }

            statuses[jobIndex] = new NativeArray<int>(1, Allocator.Persistent);
            job.Result = statuses[jobIndex];

            //Debug.Log(typeof(J).Name + " (" + jobIndex + ") " + loopIndex + "/" + recorderObjects.Count + " --> " + messages[jobIndex].Length + " packages");
            jobHandlers[jobIndex] = ScheduleJob(job, recorderObject, jobIndex);
            foundProcessor = true;
            break;
        }

        // error => negative
        // -1 => found processor
        // -2 => error && no processor
        // 0 => no processor
        // 1 => found
        return error ? (foundProcessor ? -1 : -2) : (foundProcessor ? 1 : 0);
    }

    void OnJobError()
    {
        loopIndex = 0;
    }

    bool HandleJobCompletion(int jobIndex)
    {
        bool error = false;
        //Debug.Log(typeof(J).Name + " (" + jobIndex + ") Completing Job");
        CompleteJob(jobHandlers[jobIndex], jobIndex);
        var recorder = recorderRefs[jobIndex];
        int status = statuses[jobIndex][0];
        recorder.Recorder.SyncResult((status & 1) == 1);
        recorder.Synced = (status & 2) == 2;
        if (status >= 4)
        {
            if ((status & 4) == 4)
            {
                Debug.Log(typeof(J).Name + " (" + jobIndex + ") cant find ZIP Archive");
                OnJobError();
                error = true;
            }
            if ((status & 8) == 8)
            {
                Debug.Log(typeof(J).Name + " (" + jobIndex + ") Send to Server failed");
                OnJobError();
                error = true;
            }
        }
        if (statuses[jobIndex][0] == 0)  // unhandled Error?
        {
            error = true;
            OnJobError();
        }

        return error;
    }

    bool CheckIfJobsAreCompleted()
    {
        for (int jobIndex = 0; jobIndex < jobsCount; jobIndex++)
        {
            if (!jobHandlers[jobIndex].IsCompleted)
                return false;

            if (statuses[jobIndex].Length > 0)
            {
                var error = HandleJobCompletion(jobIndex);
                statuses[jobIndex].Dispose();
                statuses[jobIndex] = default;
            }
        }
        return true;
    }

    internal bool AreAllCompleted()
    {
        return recorderObjects == null || recorderObjects.TrueForAll(O => O.Synced);
    }
    public void Iterate(bool final = false)
    {
        if (completed) return;

        if (recorderObjects == null || recorderObjects.Count == 0 || loopIndex >= recorderObjects.Count)
        {
            CheckIfJobsAreCompleted();

            if (final && AreAllCompleted())
            {
                completed = true;
            } else
            {
                NewLoop();
            }
            return;
        }

        var recorderObject = recorderObjects[loopIndex];

        if (recorderObject == null || recorderObject.Synced)
        {
            loopIndex++;
            return;
        }

        // Skip value if it is synching or has no messages
        if (recorderObject.Recorder.IsSynching || (recorderObject.Recorder.Count == 0 && !final))
        {
            loopIndex++;
            return;
        }

        // No free Job found => try again next iteration
        var r = SynRecorderObject(recorderObject, final);

        if (r == 1) loopIndex++;

    }

}
