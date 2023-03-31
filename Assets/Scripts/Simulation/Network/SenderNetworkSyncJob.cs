using System;
using System.Linq;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using UnityEngine;

public struct SenderNetworkSyncJob : INetworkSyncJob<BLEBroadcast<ulong>>
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

    public NativeArray<BLERecord<BLEBroadcast<ulong>>> messages;
    public NativeArray<BLERecord<BLEBroadcast<ulong>>> Messages { set => messages = value; }

    public NativeArray<int> result;
    public NativeArray<int> Result { set => result = value; }


    public bool final;
    public bool Final { set => final = value; }

    public async void Execute()
    {
        // chunck message in packages blocks
        BLERecord<BLEBroadcast<ulong>>[][] blocks = new BLERecord<BLEBroadcast<ulong>>[Mathf.CeilToInt((float)messages.Length / packages)][];

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
        while (i < blocks.Length && tries < 3)
        {
            success = await SimulationServerCommunication.Send(simulationId, deviceId, blocks[i], deviceType);

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
