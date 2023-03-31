using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using static AppGroupManager;

public static class SimulationServerCommunication
{
#if UNITY_EDITOR
    public readonly static string host = "localhost:8000";
#else
    public readonly static string host = "130.83.40.172";
#endif

    public static async Task<bool> Log(ulong simulationId, List<string> log)
    {
        var data = new LogRequest
        {
            log = string.Join("\n", log)
        };

        string json = JsonUtility.ToJson(data);
        return await RequestOk(simulationId, "log", json);
    }

    public static async Task<bool> Accuracies(ulong simulationId, List<AccuracyEntry> log)
    {
        var data = new AccuracyRequest
        {
            log = log.ToArray()
        };

        string json = JsonUtility.ToJson(data);
        return await RequestOk(simulationId, "accuracy", json);
    }

    public static async Task<bool> End(ulong simulationId, bool completed, float endTime, bool error = false)
    {

        var data = new EndRequest
        {
            status = completed ? "completed" : (error ? "error" : null),
            end_time = endTime
        };

        string json = JsonUtility.ToJson(data);
        return await RequestOk(simulationId, "end", json);
    }


    public static async Task<ulong> Register(int personCount, object simulationOptions)
    {
        var s = SimulationSettings.Instance;
        var data = new RegisterRequest
        {
            algorithm = s.AlgorithmName,
            device_name = SystemInfo.deviceName,
            scenario = SceneManager.GetActiveScene().name,
            seed = UnityEngine.Object.FindObjectOfType<ScenarioSeed>().Seed,
            version = Application.version,
            app_interval = s.AppUpdateInterval,
            broadcast_interval = s.BroadcastInterval,
            mode = s.Mode,
            recording = s.RecordingString,
            os = SystemInfo.operatingSystem,
            platform = Application.platform.ToString(),
            person_count = personCount,
            simulation_options = JsonUtility.ToJson(simulationOptions),
            receive_accuracy = s.ReceiveAccuarcy
        };

        string json = JsonUtility.ToJson(data);

        ulong simulationId = 0;
        using var response = await request("start", json);
        using var dataStream = response.GetResponseStream();
        using var reader = new StreamReader(dataStream);
        string responseFromServer = reader.ReadToEnd();

        if (response.StatusDescription == "OK")
        {
            simulationId = JsonUtility.FromJson<RegisterResponse>(responseFromServer).id;
            Debug.Log("SimulationId: " + simulationId);
        }

        return simulationId;
    }

    public static async Task<bool> Send<T>(ulong simulationId, int deviceId, BLERecord<BLEBroadcast<T>>[] records, BLEDeviceType type = BLEDeviceType.Device)
    {

        SendPackage[] packages = new SendPackage[records.Length];

        for (uint i = 0; i < records.Length; i++)
        {
            var record = records[i];
            packages[i] = new SendPackage
            {
                generated = record.message.generated,
                id = record.index,
                position = record.position,
                time = record.message.package.globalTime,
                uuid = record.message.package.uuid,
                value = record.message.package.value
            };
        }

        var data = new MessagesRequest<SendPackage>
        {
            device_id = deviceId,
            type = type.ToString().ToLower(),
            packages = packages
        };

        return await RequestOk(simulationId, "send", JsonUtility.ToJson(data));
    }

    public static async Task<bool> Received<T>(ulong simulationId, int deviceId, BLERecord<BLEReceive<T>>[] records, BLEDeviceType type)
    {
        ReceivedPackage[] packages = new ReceivedPackage[records.Length];

        for (uint i = 0; i < records.Length; i++)
        {
            var record = records[i];
            packages[i] = new ReceivedPackage
            {
                distance = record.message.distance,
                id = record.index,
                position = record.position,
                time = record.message.package.globalTime,
                uuid = record.message.package.uuid,
                value = record.message.package.value
            };
        }

        var data = new MessagesRequest<ReceivedPackage>
        {
            device_id = deviceId,
            type = type.ToString().ToLower(),
            packages = packages
        };

        return await RequestOk(simulationId, "received", JsonUtility.ToJson(data));
    }

    private static async Task<bool> RequestOk(ulong simulationId, string action, string json)
    {
        try
        {
            using var response = await request(simulationId + "/" + action, json);
            return true;
        } catch (Exception ex)
        {
            Debug.LogWarning("Request " + action + " failed: " + ex.Message);
            return false;
        }
    }

    private static async Task<HttpWebResponse> request(string action, string json)
    {
        var http = (HttpWebRequest)WebRequest.Create(new Uri("http://" + host + "/api/simulation/" + action));
        http.Accept = "application/json";
        http.ContentType = "application/json";
        http.Method = "POST";
        http.ServicePoint.Expect100Continue = false;
        http.Headers.Add(HttpRequestHeader.ContentEncoding, "gzip");
        http.AutomaticDecompression = DecompressionMethods.GZip;

        byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);

        using (Stream writeStream = http.GetRequestStream())
        {
            writeStream.Write(jsonToSend, 0, jsonToSend.Length);
        }

        return (HttpWebResponse)(await http.GetResponseAsync());
    }


    public static bool sendFile(string action, string path, ulong simulationId, string globalName, int globalId, int localId, char deviceType)
    {
        return sendFile(action, File.ReadAllBytes(path), Path.GetFileName(path), simulationId, globalName, globalId, localId, deviceType);
    }
    
    public static bool sendFile(
        string action,
        byte[] file,
        string filename, 
        ulong simulationId
    )
    {
        Debug.Log("Uploading " + action + " -> " + filename);
        using HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        MultipartFormDataContent form = new MultipartFormDataContent();

        form.Add(new StringContent(simulationId.ToString()), "simulation_id");
        form.Add(new ByteArrayContent(file, 0, file.Length), "file", filename);

        var response = httpClient.PostAsync("http://" + host + "/api/simulation/" + action, form).Result;

        if (!response.IsSuccessStatusCode)
        {
            Debug.LogWarning(response.StatusCode + ": " + (response.Content.ReadAsStringAsync().Result));
        }
        return response.IsSuccessStatusCode;
    }
    public static bool sendFile(
        string action, 
        byte[] file, 
        string filename, 
        ulong simulationId, 
        string globalName, 
        int globalId, 
        int localId, 
        char deviceType
    )
    {
        Debug.Log("Uploading " + action + " -> " + filename);

        using HttpClient httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        MultipartFormDataContent form = new MultipartFormDataContent();

        form.Add(new StringContent(simulationId.ToString()), "simulation_id");
        form.Add(new StringContent(globalName.ToString()), "name");
        form.Add(new StringContent(globalId.ToString()), "global_id");
        form.Add(new StringContent(localId.ToString()), "local_id");
        form.Add(new StringContent(deviceType.ToString()), "device_type");
        form.Add(new ByteArrayContent(file, 0, file.Length), "file", filename);

        var response = httpClient.PostAsync("http://" + host + "/api/simulation/" + action, form).Result;

        if (! response.IsSuccessStatusCode)
        {
            Debug.LogWarning(response.StatusCode + ": " + (response.Content.ReadAsStringAsync().Result));
        }
        return response.IsSuccessStatusCode;
    }

}
