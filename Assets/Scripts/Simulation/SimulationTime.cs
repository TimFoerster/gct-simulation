using UnityEngine;

public class SimulationTime : MonoBehaviour
{
    float currentTime = 0;
    uint count = 0;
    void FixedUpdate()
    {
        count++;
        currentTime = Time.fixedDeltaTime * count;
    }

    public float time => currentTime;
    public uint Counter => count;

    public float fixedDeltaTime => Time.fixedDeltaTime;
}
