using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class OverlayHandler : MonoBehaviour
{
    [SerializeField]
    Camera uiCamera;

    [SerializeField]
    AppHandler appHandler;

    [SerializeField]
    GameObject probeStatistic;

    OverlayType overlayType = OverlayType.Statistics;

    public OverlayType OverlayType => overlayType;

    int val;

    void Start()
    {
        OnOverlayChange(GetComponent<TMP_Dropdown>().value);
    }

    public void OnOverlayChange(int i)
    {
        val = i;
        if (overlayType == OverlayType.Statistics)
        {
            DisableStatisticLayer();
        }

        overlayType = (OverlayType)val;

        switch (overlayType)
        {
            case OverlayType.None:
                break;
            case OverlayType.Statistics:
                EnableStatisticLayer();
                break;
            case OverlayType.Groups:
                // TODO
                break;
        }


        appHandler.OnOverlayTypeChanged(overlayType);
    }

    void EnableStatisticLayer()
    {
        probeStatistic.SetActive(true);
        BleProbeStatistics(true);
        BleProbes(true);
        uiCamera.cullingMask |= 1 << LayerMask.NameToLayer("Statistic");
    }

    void DisableStatisticLayer()
    {
        BleProbes(false);
        BleProbeStatistics(false);
        uiCamera.cullingMask &= ~(1 << LayerMask.NameToLayer("Statistic"));
        probeStatistic.SetActive(false);

    }

    void BleProbeStatistics(bool value)
    {
        var probeStatistics = GameObject.FindObjectsOfType<BLEProbesStatistic>();
        foreach (var probe in probeStatistics)
        {
            probe.enabled = value;
        }
    }

    void BleProbes(bool value)
    {
        var probes = GameObject.FindObjectsOfType<BLEProbe>();
        foreach (var probe in probes)
        {
            probe.enabled = value;
        }
    }

}
