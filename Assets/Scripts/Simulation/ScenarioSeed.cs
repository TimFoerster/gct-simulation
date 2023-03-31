using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ScenarioSeed : MonoBehaviour
{
    [SerializeField]
    int seed;

    [SerializeField]
    TMP_Text info;

    public int Seed => seed;

    public void Init(int seed)
    {
        Random.InitState(seed);
    }
    private void Awake()
    {
        int ssSeed = SimulationSettings.Instance.Seed;

        if (ssSeed == default)
        {
            if (seed == default)
            {
                var r = new System.Random();
                r.Next();
                seed = r.Next(int.MinValue, int.MaxValue);
            }
        } else
        {
            seed = ssSeed;
        }

        Init(seed);

    }

    void Start()
    {
        if (info)
            info.text = "Seed: " + seed;
    }
}
