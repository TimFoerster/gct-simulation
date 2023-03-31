using System.Linq;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public interface INetworkSyncJob<T> : ISyncJob<T>
{
    int Index { set; }

}
