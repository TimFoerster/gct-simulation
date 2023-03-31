using System.Linq;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public struct ReceiverNetworkSyncJob : INetworkSyncJob<BLEReceive<ulong>>
{
    const int packages = 1_000_000;

    int index;
    public int Index { set => index = value; }

    int deviceId;
    public int DeviceId { set => deviceId = value; }

    ulong simulationId;
    public ulong SimulationId { set => simulationId = value; }

    BLEDeviceType deviceType;
    public BLEDeviceType DeviceType { set => deviceType = value; }

    public NativeArray<BLERecord<BLEReceive<ulong>>> messages;
    public NativeArray<BLERecord<BLEReceive<ulong>>> Messages {set => messages = value; }

    public NativeArray<int> result;
    public NativeArray<int> Result { set => result = value; }

    public bool final;
    public bool Final { set => final = value; }

    public async void Execute()
    {
        // chunck message in packages blocks
        BLERecord<BLEReceive<ulong>>[][] blocks = new BLERecord<BLEReceive<ulong>>[Mathf.CeilToInt( (float)messages.Length / packages)][];

        int i;

        for (i = 0; i < blocks.Length; i++)
        {
            int remaining = messages.Length - i * packages;
            blocks[i] = messages.Skip(i * packages).Take(remaining > packages ? packages : remaining).ToArray();
        }

        // sync with remote
        int tries = 0;
        i = 0;
        bool success = false;
        int sum = 0;
        while (i < blocks.Length && tries < 10)
        {
            success = await SimulationServerCommunication.Received(simulationId, deviceId, blocks[i], deviceType);

            if (success)
            {
                i++;
                tries = 0;
                sum += blocks[i].Length;
                continue;
            }

            tries++;
            Thread.Sleep(tries * 50);
        }


        result[0] = success ? 1 : -1;
    }



}
