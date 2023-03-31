using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;


public class SimulationSychronizer : MonoBehaviour
{
    SychronizerOrganisator sychronizerOrganisator;
    ulong simulationId = 0;

    // every 30 min take a screenshot
    // const float screenshotInterval = 30 * 60;
    const float screenshotInterval = float.PositiveInfinity;
    float nextScreenshot = screenshotInterval;

    float simulationEnd;

    public bool success = false;

    public float SimulationEnd { set => simulationEnd = value; }

    [SerializeField]
    List<BLESender> senders;

    [SerializeField]
    List<BLEReceiver> receivers;

    [SerializeField]
    List<TracingApp> tracingApps;

    [SerializeField]
    AppGroupManager appGroupManager;

    [SerializeField] SimulationLogger simulationLogger;

    public ulong SimulationId => simulationId;

    string screenshotPath = "";
    public bool completed;

    bool finalSync = false;
    float endtime;

    [SerializeField]
    SimulationTime time;

    bool IsCompleted =>
        completed &&
        sychronizerOrganisator.completed &&
        appGroupManager.completed;


    public void Exit(bool error = false)
    {
        endtime = time.time;
        Final(error);
    }

    public async void SyncData(float endTime)
    {
        finalSync = true;
        endtime = endTime;
        StartCoroutine(UploadScreenshots());
        StartCoroutine(WriteAppgroupData(true));
        if (sychronizerOrganisator != null)
        {
            sychronizerOrganisator.FinalSync();
        }

        await appGroupManager.WriteCsvFile();
        var tries = 0;
        while(true)
        {
            if (tries > 5)
            {
                Debug.LogWarning("Accuracies could not be synced");
                appGroupManager.completed = true;
                break;
            }

            if (await appGroupManager.SyncAccuracyAsync(this))
                break;

            tries++;
        }
    }

    async protected void Final(bool error)
    {
        if (simulationId > 0)
        {
            var tries = 0;
            do
            {
                if (tries++ > 10)
                {
                    Debug.LogWarning("Simulation End sending failed 10 times, check server");
                    break;
                }

                var ending = await SimulationServerCommunication.End(simulationId, success, endtime, error);

                if (ending)
                {
                    Debug.Log("Simulation End sent");
                    break;
                }

                tries++;

            } while (true);
        } 

        completed = true;
    }

    public bool CanQuit => simulationLogger.completed;

    public async Task QuitAsync()
    {
        int tries = 0;
        do
        {
            if (tries++ > 10)
            {
                Debug.LogWarning("Simulation Log sending failed 10 times, check server");
                simulationLogger.completed = true;
                break;
            }

            var logSync = await simulationLogger.SyncAsync();
            if (logSync)
            {
                Debug.Log("Simulation Log sent");
                break;
            }
        } while (true);
    }

    public async Task RegisterSimulationAsync(int personCount, object simulationOptions)
    {
        if (SimulationSettings.Instance.OfflineMode)
        {
            DisableRecording();
            completed = true;
        } else
        {
            if (await Register(personCount, simulationOptions) == 0)
            {
                DisableRecording();
                completed = true;
            }
        }
    }

    float nextGroupWrite = 0;
    bool groupLock = false;


    private void FixedUpdate()
    {
        Syncronize();

        if (nextGroupWrite <= Time.time && !groupLock && appGroupManager != null)
        {
            nextGroupWrite = Time.time + 60;
            StartCoroutine(WriteAppgroupData());
            _ = appGroupManager.WriteCsvFile();

        }

        if (finalSync) return;

        if (nextScreenshot <= time.time)
        {
            Logger.Log("Simulation Progress " + (time.time * 100 / simulationEnd).ToString("F") + " % (" + time.time + "/" + simulationEnd + ") Speed: " + Time.timeScale.ToString() + " x");

            if (!SimulationSettings.Instance.IsHeadless && Directory.Exists(screenshotPath))
            {
                StartCoroutine(UploadScreenshots());
                var path = Path.Combine(screenshotPath, nextScreenshot.ToString() + ".png");
                ScreenCapture.CaptureScreenshot(path, 12);
            }
            nextScreenshot += screenshotInterval;
        }
    }

    IEnumerator UploadScreenshots()
    {
        if (Directory.Exists(screenshotPath))
        {
            foreach (var screenshot in Directory.GetFiles(screenshotPath, "*.png"))
            {
                List<IMultipartFormSection> formData = new List<IMultipartFormSection>();

                formData.Add(new MultipartFormFileSection("file", File.ReadAllBytes(screenshot), Path.GetFileName(screenshot), "image/png"));
                UnityWebRequest www = UnityWebRequest.Post("http://"+ SimulationServerCommunication.host + "/api/simulation/" + simulationId + "/screenshot", formData);
                yield return www.SendWebRequest();

                if (www.responseCode >= 400)
                {
                    Debug.Log(www.error);
                    continue;
                }

                Debug.Log("Screenshot uploaded");
                File.Delete(screenshot);
            }
        }
    }

    string[] appGroupFields = new string[] { "local_group_id", "time", "mean", "apps_count", "max_diff" };


    IEnumerator WriteAppgroupData(bool final = false)
    {
        if (simulationId == default) yield break;
        groupLock = true;
        var dataToSync = appGroupManager.ItemsToSync();
        var writeCount = 1024;
        if (Directory.Exists(screenshotPath) && dataToSync.Count > 0)
        {
            var fileName = Path.Combine(screenshotPath, "groups.csv");
            var exists = File.Exists(fileName);
            while (dataToSync.Count > 0)
            {
                try
                {
                    using var writer = new CsvStreamWriter(fileName, true);
                    
                    if (!exists)
                        writer.WriteHeader(appGroupFields);

                    int writeCurrent = dataToSync.Count > writeCount ? writeCount : dataToSync.Count;
                    writer.WriteLines(
                        dataToSync.Take(writeCount).Select(record =>
                            string.Format(
                                writer.FormatProvider,
                                "{0},{1},{2},{3},{4}",
                                record.id,
                                record.time,
                                record.mean,
                                record.appsCount,
                                record.max_diff
                            )
                        )
                    );
                    dataToSync.RemoveRange(0, writeCurrent);
                    appGroupManager.CompletedSync(writeCurrent);
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(ex.Message + "\n" + ex.StackTrace, this);
                }
                yield return null;
            }
        }

        groupLock = false;

    }


    void Syncronize()
    {

        if (finalSync)
        {
            if (sychronizerOrganisator == null || sychronizerOrganisator.completed || simulationId == 0)
            {
                completed = true;
                return;
            }

            sychronizerOrganisator.Iterate();
            return;
        }

        if (time.time < 200 || simulationId == 0 || sychronizerOrganisator == null)
            return;

        if (sychronizerOrganisator.completed)
        {
            sychronizerOrganisator.NewLoop();
            return;
        }

        sychronizerOrganisator.Iterate();
    }

    bool recording = true;

    async Task<ulong> Register(int personCount, object simulationOptions)
    {
        try
        {
            simulationId = await SimulationServerCommunication.Register(personCount, simulationOptions);
            screenshotPath = Path.Combine(Application.dataPath, "../Data", simulationId.ToString());
            appGroupManager.SetSimulationId(simulationId);
            simulationLogger.SetSimulationId(simulationId);
            OnEnable();
            return simulationId;
        }
        catch (Exception e)
        {
            DisableRecording();
            Debug.LogWarning("Unable to register Simulation: " + e.Message);
            return 0;
        }
        
    }

    void DisableRecording()
    {
        foreach (var sender in senders)
            sender.DisableRecording();

        foreach (var receiver in receivers)
            receiver.DisableRecording();

        foreach (var app in tracingApps)
            app.DisableRecording();

        recording = false;
        senders.Clear();
        receivers.Clear();
        tracingApps.Clear();

        if (appGroupManager != null)
            appGroupManager.DisableRecording();

        simulationLogger.Disable();

    }

    void OnDisable()
    {
        sychronizerOrganisator?.Destruct();
        if (screenshotPath != null)
        {
            var e = UploadScreenshots();
            while (e.MoveNext()) { }
        }
    }

    void OnEnable()
    {
        groupLock = false;

        if (simulationId != 0 && sychronizerOrganisator == null)
        {
            sychronizerOrganisator = new SychronizerOrganisator(simulationId, senders, receivers, tracingApps);
        }
        simulationSettings = SimulationSettings.Instance;
    }

    SimulationSettings simulationSettings;

    public void Register(BLESender sender)
    {
        if (!recording || !simulationSettings.RecordAppSenders)
            sender.DisableRecording();

        if (sender.IsRecording)
        {
            senders.Add(sender);
            sychronizerOrganisator?.NewLoop();
        }

    }

    public void Register(BLEReceiver receiver)
    {
        if (!recording || // No Recording
            (!simulationSettings.RecordBeaconReceivers && receiver.deviceType == BLEDeviceType.Beacon) || // Is not a beacon with becon logging
            (!simulationSettings.RecordAppReceivers && receiver.deviceType == BLEDeviceType.Device)
        )
            receiver.DisableRecording();

        if (!recording || !simulationSettings.RecordGroups)
            receiver.DisableGroupRecording();


        if (receiver.IsRecording)
        {
            receivers.Add(receiver);
            sychronizerOrganisator?.NewLoop();
        }

    }


    internal void Deregister(BLESender sender)
    {
        if (sender.IsRecording)
        {
            senders.Remove(sender);
            sychronizerOrganisator?.NewLoop();
        }
    }

    internal void Deregister(BLEReceiver receiver)
    {
        if (receiver.IsRecording)
        {
            receivers.Remove(receiver);
            sychronizerOrganisator?.NewLoop();
        }
    }

    internal Task<bool> Log(List<string> log)
    {
        return SimulationServerCommunication.Log(simulationId, log);
    }

    internal Task<bool> Accuracy(List<AccuracyEntry> log)
    {
        return SimulationServerCommunication.Accuracies(simulationId, log);
    }

    internal void Register(TracingApp tracingApp)
    {
        if (!recording || !simulationSettings.RecordGroups || !tracingApp.recordingGroups)
        {
            tracingApp.DisableRecording();
        }

        if (tracingApp.IsRecording)
        {
            tracingApps.Add(tracingApp);
            sychronizerOrganisator?.NewLoop();
        }
    }

    internal void Deregister(TracingApp tracingApp)
    {
        if (tracingApp.IsRecording)
        {
            tracingApps.Remove(tracingApp);
            sychronizerOrganisator?.NewLoop();
        }
    }
}
