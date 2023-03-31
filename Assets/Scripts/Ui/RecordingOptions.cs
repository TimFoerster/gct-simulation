using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RecordingOptions : MonoBehaviour
{
    [SerializeField] Toggle accuracy;
    [SerializeField] Toggle beacon;
    [SerializeField] Toggle appReceive;
    [SerializeField] Toggle appGroups;
    [SerializeField] Toggle appSends;
    SimulationSettings simulationSettings;
    private void Start()
    {
        simulationSettings = SimulationSettings.Instance;

        accuracy.SetIsOnWithoutNotify(simulationSettings.RecordAccuracy);
        beacon.SetIsOnWithoutNotify(simulationSettings.RecordBeaconReceivers);
        appReceive.SetIsOnWithoutNotify(simulationSettings.RecordAppReceivers);
        appGroups.SetIsOnWithoutNotify(simulationSettings.RecordGroups);
        appSends.SetIsOnWithoutNotify(simulationSettings.RecordAppSenders);
    }

    public void OnChange()
    {
        simulationSettings.RecordAccuracy = accuracy.isOn;
        simulationSettings.RecordBeaconReceivers = beacon.isOn;
        simulationSettings.RecordAppReceivers = appReceive.isOn;
        simulationSettings.RecordGroups = appGroups.isOn;
        simulationSettings.RecordAppSenders = appSends.isOn;

        simulationSettings.UpdateRecordingString();
    }
}
