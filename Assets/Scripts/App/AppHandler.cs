using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;


public class AppHandler : MonoBehaviour
{
    [SerializeField]
    OverlayHandler overlayHandler;

    [SerializeField]
    TimeslotEntries<TracingApp>[] activeApps;

    [SerializeField, ReadOnly] int devicesCount;

    [SerializeField]
    SimulationTime time;

    [SerializeField]
    SimulationSychronizer simulationSychronizer;

    [SerializeField]
    AppGroupManager appGroupManager;

    int timeSlot = 0;
    float interval;
    public int maxIntervalIndex;

    public OverlayType OverlayType => overlayHandler.OverlayType;


    private void Awake()
    {
        interval = SimulationSettings.Instance.AppUpdateInterval;
        maxIntervalIndex = (int)(interval / Time.fixedDeltaTime);

        activeApps = new TimeslotEntries<TracingApp>[maxIntervalIndex];
        for (int i = 0; i < activeApps.Length; i++)
        {
            activeApps[i] = new TimeslotEntries<TracingApp>();
        }
    }

    void FixedUpdate()
    {
        foreach (var app in activeApps[timeSlot])
        {
            var ids = app.OnAppTrigger();
            ReceivedUuids(app, ids);
        }

        if (OverlayType == OverlayType.Groups)
            appGroupManager.Updated(activeApps[timeSlot]);

        appGroupManager.UpdateAccuracy(activeApps[timeSlot]);

        timeSlot = (timeSlot + 1) % maxIntervalIndex;
    }


    public void SetDevicesCount(int count)
    {
        devicesCount = count;
        appGroupManager.SetDeviceCount(count);
    }

    internal void OnOverlayTypeChanged(OverlayType overlayType)
    {
        foreach (var slot in activeApps)
        {
            foreach (var app in slot)
            {
                app.OverlayTypeChanged(overlayType);
            }
        }
    }

    public void AddApp(TracingApp app)
    {
        activeApps[app.Timeslot].Add(app);
        appGroupManager.Add(app);
    }

    internal void ReceivedUuids(TracingApp app, int[] ids)
    {
        appGroupManager.ReceivedIds(app, ids);
    }

    internal void RemoveApp(TracingApp app)
    {
        activeApps[app.Timeslot].Remove(app);
        appGroupManager.Remove(app);
    }

    internal Color GetGroupColor(TracingApp app) => appGroupManager != null ? 
        appGroupManager.GetGroupColor(app): Color.black;
}
 