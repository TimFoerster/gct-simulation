using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class TrainWorld : MonoBehaviour
{

    Station[] stations;
    float[] stationProperbilities;

    float spawnSum;
    private void Awake()
    {
        stations = GetComponentsInChildren<Station>();

        stationProperbilities = new float[stations.Length];
        stationProperbilities = stations.Select(s => s.SpawnModifier).ToArray();   
        spawnSum = stationProperbilities.Sum();
        var sum = 0f;
        for (int i = 0; i < stationProperbilities.Length; i++)
        {
            stations[i].Index = i;
            sum += stationProperbilities[i] / spawnSum;
            stationProperbilities[i] = sum;
        }
    }

    public int StationCount => stations.Length;

    public Station GetRandomStart(RandomNumberGenerator rng)
    {
        var r = rng.Range();
        for (int ni = 0; ni < stationProperbilities.Length; ni++)
        {
            if (r < stationProperbilities[ni])
                return stations[ni];
        }

        return stations[stations.Length - 1];
    }

    public Station GetRandomDestination(RandomNumberGenerator rng, Station start)
    {
        var r = rng.Range();
        for (int i = 0; i < stations.Length; i++)
        {
            if (stationProperbilities[i] < r) continue;

            if (start == stations[i]) continue; 

            return stations[i];
        }

        if (start == stations[stations.Length - 1]) 
            return stations[stations.Length - 2];


        return stations[stations.Length - 1];
    }

}
