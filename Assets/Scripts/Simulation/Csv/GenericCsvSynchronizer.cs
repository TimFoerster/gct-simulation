using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.Networking;

public abstract class GenericCsvSychronizer<O, T, TJ, J> : JobSychronizerOrganisator<O, T, TJ, J> 
    where O : MonoBehaviour, IHasRecorder<T> 
    where J : ICsvSyncJob<TJ>
{
    NativeArray<byte>[] pathByte;
    NativeArray<byte>[] globalName;
    NativeArray<byte>[] localName;
    protected string filename;
    protected string action;

    public GenericCsvSychronizer(ulong simulationId, List<O> objects, int jobs = 2) : base(simulationId, objects, jobs)
    {
        pathByte = new NativeArray<byte>[jobs];
        globalName = new NativeArray<byte>[jobs];
        localName = new NativeArray<byte>[jobs];
    }

    public override void Init()
    {
        base.Init();
        Directory.CreateDirectory(StoragePath);
    }

    private string GetCsvFilename(string globalName, int globalId, int localId)
    {
        return string.Format("{0}-{1}-{2}.csv", globalName, globalId, localId);
    }


    private string StoragePath => Path.Combine(Application.dataPath, "../Data", simulationId.ToString(), filename);

    private string GetPath(string globalName, int globalId, int localId)
    {
        var fn = GetCsvFilename(globalName, globalId, localId);
        return StoragePath   + "/" + fn ;
    }

    abstract protected JobHandle ScheduleCsvJob(J job, int jobIndex);

    protected override JobHandle ScheduleJob(J job, O recorderObject, int jobIndex)
    {

        var p = GetPath(recorderObject.GlobalName, recorderObject.GlobalIndex, recorderObject.LocalIndex);

        pathByte[jobIndex] = new NativeArray<byte>(p.Length, Allocator.Persistent);
        pathByte[jobIndex].CopyFrom(Encoding.ASCII.GetBytes(p));
        job.Path = pathByte[jobIndex];
        
        job.GlobalId = recorderObject.GlobalIndex;
        var globalNameByte = Encoding.UTF8.GetBytes(recorderObject.GlobalName);
        globalName[jobIndex] = new NativeArray<byte>(globalNameByte.Length, Allocator.Persistent);
        globalName[jobIndex].CopyFrom(globalNameByte);
        job.GlobalName = globalName[jobIndex];

        job.LocalId = recorderObject.LocalIndex;
        var localNameByte = Encoding.UTF8.GetBytes(recorderObject.LocalName);
        localName[jobIndex] = new NativeArray<byte>(localNameByte.Length, Allocator.Persistent);
        localName[jobIndex].CopyFrom(localNameByte);
        job.LocalName = localName[jobIndex];

        return ScheduleCsvJob(job, jobIndex);
    }

    protected override void CompleteJob(JobHandle job, int jobIndex)
    {
        job.Complete();
        if (pathByte[jobIndex] != default)
        {
            pathByte[jobIndex].Dispose();
            pathByte[jobIndex] = default;
        }
        if (globalName[jobIndex] != default)
        {
            globalName[jobIndex].Dispose();
            globalName[jobIndex] = default;
        }
        if (localName[jobIndex] != default)
        {
            localName[jobIndex].Dispose();
            localName[jobIndex] = default;
        }
    }


    /*
    public void Final()
    {
        var path = new DirectoryInfo(StoragePath);
        foreach (var file in path.GetFiles("*.csv"))
        {
            using (FileStream fs = new FileStream(file.FullName + ".zip", FileMode.Create))
            {
                using ZipArchive arch = new ZipArchive(fs, ZipArchiveMode.Create);
                arch.CreateEntryFromFile(file.FullName, file.Name);
            };
            file.Delete();
        }
        
        Upload();
    }

    private void Upload()
    {
        var path = new DirectoryInfo(StoragePath);
        foreach (var file in path.GetFiles("*.zip"))
        {
            Logger.Log("Uploading " + file.Name);

            List<IMultipartFormSection> formData = new List<IMultipartFormSection>();
            formData.Add(new MultipartFormFileSection("file", File.ReadAllBytes(file.FullName), file.Name, "application/zip"));


        var www = UnityWebRequest.Post("http://"+ SimulationServerCommunication.host + "/api/simulation/" + simulationId + "/" + action + "/file", formData);
            www.SendWebRequest();

            while (!www.isDone) { }
            if (www.result != UnityWebRequest.Result.Success)
            {
                Logger.Log(www.error);
            } else
            {
                file.Delete();
            }
        }
            
    }
    */

}
