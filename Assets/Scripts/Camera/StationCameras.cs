using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StationCameras : MonoBehaviour
{


    // Start is called before the first frame update
    void Start()
    {
        var stations = GetComponentsInChildren<Camera>();
        var count = stations.Length;

        var baseRect = stations[0].rect;
        var columns = (int)(1 / baseRect.width);
        for (int i = 0; i < count; i++)
        {
            var station = stations[i];

            station.rect = new Rect(station.rect.width * (i % columns), station.rect.height * (i / columns), station.rect.width, station.rect.height);
            station.enabled = false;
        }
    }
}