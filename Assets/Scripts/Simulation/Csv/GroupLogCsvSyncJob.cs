using System;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using Unity.Collections;
using UnityEngine;

public struct GroupLogSyncJob : ICsvSyncJob<GroupLogCsvEntry>
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

    public NativeArray<BLERecord<GroupLogCsvEntry>> messages;
    public NativeArray<BLERecord<GroupLogCsvEntry>> Messages { set => messages = value; }

    public NativeArray<int> result;
    public NativeArray<int> Result { set => result = value; }

    public NativeArray<byte> path;
    public NativeArray<byte> Path { set => path = value; }

    public bool final;
    public bool Final { set => final = value; }

    public NativeArray<GroupLogDevice> devices;

    public void Execute()
    
    {
        var fields = new string[]{ "entry", "t", "time", "gid", "devices", "x", "y", "z", "received"}; 
        var p = Encoding.ASCII.GetString(path.ToArray());
        var fileName = System.IO.Path.GetFileName(p);
        var exists = File.Exists(p);
        var status = 0;
        var localDevices = devices;

        if (!exists)
        {
            using var writer = new CsvStreamWriter(p);
            writer.WriteHeader(fields);
            exists = true;
        }

        if (messages.Length > 0)
        {
            try
            {
                using var writer = new CsvStreamWriter(p, true);

            var skipCounter = 0;
            writer.WriteLines(
                messages.Select(record =>
                {
                    var numberOfEntries = record.message.receivedUuids;
                    var elements = localDevices.Skip(skipCounter).Take(numberOfEntries);
                    skipCounter += numberOfEntries;

                    return string.Format(
                        writer.FormatProvider,
                        "{0},{1},{2},{3},{4},{5},{6},{7},{8}",
                        record.index,
                        record.message.t,
                        record.message.time,
                        record.message.gid,
                        record.message.devices,
                        record.position.x,
                        record.position.y,
                        record.position.z,
                        string.Join("|", elements.Select(r =>
                            string.Format(
                                writer.FormatProvider,
                                "{0}:{1}",
                                r.remote_uuid, r.iterations)
                        )) // "0:5|534:34|...
                    );
                }
                        
                )
            );

            status = 1;
        } catch (IOException e)
        {
            Debug.LogError(e.Message);
            final = false;
            status = 0;
        }
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
            status = +4;
        }
        // send file to server
        else if (SimulationServerCommunication.sendFile(
            simulationId + "/group/file", 
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
        }
        else
        {
            Debug.Log("ZIP Sending failed: " + zipArchive);
            status = +8;
        }
        

        result[0] = status; 
        // 1 => written to csv
        // 2 => synced to server
        // 4 => cant find zip archive
        // 8 => failed to write file to the server;
    }
}
