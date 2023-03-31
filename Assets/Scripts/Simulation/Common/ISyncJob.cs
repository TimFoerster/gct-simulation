using System.Linq;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public interface ISyncJob<T> : IJob
{
    int DeviceId { set; }
    ulong SimulationId { set; }
    BLEDeviceType DeviceType { set; }
    NativeArray<BLERecord<T>> Messages { set; }
    NativeArray<int> Result { set; }
    bool Final { set; }
}
