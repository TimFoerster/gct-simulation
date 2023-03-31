using System.Linq;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public interface ICsvSyncJob<T> : ISyncJob<T>
{

    public NativeArray<byte> GlobalName { set ; }
    public int GlobalId { set; }

    public NativeArray<byte> LocalName { set ; }
    public int LocalId { set; }


    NativeArray<byte> Path { set; }

}
