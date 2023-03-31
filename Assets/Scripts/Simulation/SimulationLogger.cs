using System;
using System.Collections;
using System.Collections.Generic;
using System.IO.Compression;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using System.Linq;
using System.Diagnostics;

[Serializable]
public struct LoggerEntry
{
    public uint index;
    public uint timestep;
    public float time;
    public string level;
    public string message;
    public string details;
}

public class SimulationLogger : MonoBehaviour
{
    [SerializeField] uint loggerIndex = 0;

    [SerializeField] int sendIndex = 0;

    [SerializeField] List<LoggerEntry> log;

    [SerializeField] GenericSimulation genericSimulation;
    [SerializeField] SimulationSychronizer simulationSychronizer;
    [SerializeField] SimulationTime time;
    [SerializeField] GameTime gameTime;
    Logger logger;
    public bool completed = false;

    private void Awake()
    {
        Application.logMessageReceived += HandleNewLogEntry;
    }

    private void OnEnable()
    {
        logger = Logger.Init(gameTime, this);
    }

    protected void Log(string message) => Logger.Log(message);

    bool enableLogging = true;

    async void HandleNewLogEntry(string logString, string stackTrace, LogType type)
    {
        if (!enableLogging) return;


        log.Add(new LoggerEntry
        {
            index = ++loggerIndex,
            timestep = time.Counter,
            time = time.time,
            level = type.ToString(),
            message = escapeLogString(logString),
            details = type == LogType.Error ? escapeLogString(stackTrace) : null
        });


        if (type == LogType.Error)
        {
            Log("Exception: ");
            Log(stackTrace);
            Log("Exiting...");
            genericSimulation.OnError();
        }

        if (log.Count > 100)
            await WriteCsvFile();
    }

    protected string escapeLogString(string message)
    {
        return message.Replace(Environment.NewLine, "\\n");
    }
    public void LogSimulation(string message, string level = "sim")
    {
        if (!enableLogging) return;

        log.Add(new LoggerEntry
        {
            index = ++loggerIndex,
            timestep = time.Counter,
            time = time.time,
            level = level,
            message = escapeLogString(message),
            details = null
        });

    }

    public void Disable()
    {
        enableLogging = false;
        completed = true;
    }

    string filename = "log.csv";
    ulong simId;
    string filePath;

    public async void SetSimulationId(ulong id)
    {
        simId = id;
        filePath = Path.Combine(Application.dataPath, "../Data", simId.ToString(), filename);
        await CreateCsvFile();
    }

    public async Task<bool> SyncAsync()
    {
        await WriteCsvFile(true);
        var zipArchive = filename + ".zip";
        using (FileStream fs = new FileStream(zipArchive, FileMode.Create))
        {
            using ZipArchive arch = new ZipArchive(fs, ZipArchiveMode.Create);
            arch.CreateEntryFromFile(filePath, filename);
        };

        var okay = SimulationServerCommunication.sendFile(
            simId + "/log/file",
            await File.ReadAllBytesAsync(zipArchive),
            zipArchive,
            simId
        );

        if (okay)
        {
            File.Delete(filePath);
            File.Delete(zipArchive);
            completed = true;
        }

        return okay;
    }


    public async Task WriteCsvFile(bool force = false)
    {
        if ((sendIndex > 0 && !force) || log.Count == 0 || simId == default) return;

        using (var writer = new CsvStreamWriter(filePath, true))
        {
            sendIndex = log.Count;

            await writer.WriteLinesAsync(
                log.Select(record =>
                    string.Format(
                        writer.FormatProvider,
                        "{0},{1},{2},{3},\"{4}\",\"{5}\"",
                        record.index,
                        record.timestep,
                        record.time,
                        record.level,
                        record.message,
                        record.details
                    )
                )
            );

            log.RemoveRange(0, sendIndex);
            sendIndex = 0;
        }
    }

    public async Task CreateCsvFile()
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "../Data", simId.ToString()));

        var fields = new string[] { "index", "timestep", "time", "level", "message", "details" };

        using var writer = new CsvStreamWriter(filePath);
        await writer.WriteHeaderAsync(fields);
    }

}
