using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

public struct SenderCsvSyncJob : ICsvSyncJob<BLEBroadcast<ulong>>
{
    int globalId;
    public int GlobalId { set => globalId = value; }

    public NativeArray<byte> globalName;
    public NativeArray<byte> GlobalName { set => globalName = value; }

    int localId;
    public int LocalId { set => localId = value; }


    public NativeArray<byte> localName;
    public NativeArray<byte> LocalName { set => localName = value; }

    ulong simulationId;
    public ulong SimulationId { set => simulationId = value; }


    int deviceId;
    public int DeviceId { set => deviceId = value; }

    BLEDeviceType deviceType;
    public BLEDeviceType DeviceType { set => deviceType = value; }

    public NativeArray<BLERecord<BLEBroadcast<ulong>>> messages;
    public NativeArray<BLERecord<BLEBroadcast<ulong>>> Messages { set => messages = value; }

    public NativeArray<int> result;
    public NativeArray<int> Result { set => result = value; }

    public NativeArray<byte> path;
    public NativeArray<byte> Path { set => path = value; }

    public bool final;
    public bool Final { set => final = value; }

    public void Execute()
    
    {
        var fields = new string[]{ "package_id", "time", "uuid", "x", "y", "z", "value", "generated" }; 
        //var fields = new string[]{ "simulation_id", "global_name", "global_id", "local_id", "package_id", "type", "time", "uuid", "x", "y", "z", "value", "generated" }; 
        var p = Encoding.ASCII.GetString(path.ToArray());
        var fileName = System.IO.Path.GetFileName(p);
        var exists = File.Exists(p);
        var status = 0;

        if (!exists)
        {
            using var writer = new CsvStreamWriter(p);
            writer.WriteHeader(fields);
            exists = true;
        }

        if (messages.Length > 0)
        {

            using var writer = new CsvStreamWriter(p, true);
            writer.WriteLines(
                messages.Select(record =>
                    String.Format(
                        CultureInfo.InvariantCulture,
                        "{0},{1},{2},{3},{4},{5},{6},{7}",
                        record.index,
                        record.message.package.globalTime,
                        record.message.package.uuid,
                        record.position.x,
                        record.position.y,
                        record.position.z,
                        record.message.package.value,
                        record.message.generated ? "1" : "0"
                    )
                )
            );

            status = 1;
        }

        if (!final)
        {
            result[0] = status;
            return;
        }

        // Zip and send to server
        var zipArchive = p + ".zip";

        // if csv file exists => zip it
        if (exists)
        {
            using (FileStream fs = new FileStream(zipArchive, FileMode.Create))
            {
                using ZipArchive arch = new ZipArchive(fs, ZipArchiveMode.Create);
                arch.CreateEntryFromFile(p, fileName);
            };

            // remove csv file
            File.Delete(p);

            Debug.Log("Created ZIP: " + zipArchive);
        }

        // Send the zip file to the server
        // if this zip file is missing, just ignore, nothing we could do.
        if (!File.Exists(zipArchive))
        {
            Debug.LogWarning("Cant find: " + zipArchive);
            status += 4;
        }
        // send file to server
        else if (SimulationServerCommunication.sendFile(
            simulationId + "/send/file", 
            zipArchive,
            simulationId,
            Encoding.UTF8.GetString(globalName.ToArray()),
            globalId,
            localId,
            char.ToLower(deviceType.ToString()[0])))
        {
            status += 2;
            File.Delete(zipArchive);
            Debug.Log("Deleted ZIP after send: " + zipArchive);
        } else
        {
            Debug.Log("ZIP Sending failed: " + zipArchive);
            status = +8;
        }

        result[0] = status;
    }
}
