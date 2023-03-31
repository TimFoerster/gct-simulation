using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

public class TrainSpawn : MonoBehaviour
{
    public float spawnInterval;

    public GameObject trainsParent;

    public TrainDirection direction;

    public StationPlatform[] platforms;

    int trainIndex;
    public TrainLogic[] trains;

    public float nextSpawn;

    [SerializeField]
    Transform trackEnd;
    public Vector3 TrackEnd => trackEnd.position;

    [SerializeField]
    SimulationTime time;

    private void Awake()
    {
        platforms = direction == TrainDirection.East ? getStationPlatforms("PlatformN") : getStationPlatforms("PlatformS");
        time = FindObjectOfType<SimulationTime>();
    }

    private void FixedUpdate()
    {
        if (nextSpawn > time.time) return;

        Spawn();

        if (trainIndex < trains.Length)
            nextSpawn += spawnInterval;
        else
            nextSpawn = float.PositiveInfinity;

    }

    void Spawn()
    {
        trains[trainIndex].gameObject.SetActive(true);
        trainIndex++;
    }

    StationPlatform[] getStationPlatforms(string tag) {
        return GameObject.FindGameObjectsWithTag(tag)
                .Select(p => p.GetComponent<StationPlatform>())
                .OrderBy(p => (p.transform.position - transform.position).sqrMagnitude)
                .ToArray();
    }
}
