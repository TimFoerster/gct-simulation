using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System;
using Unity.Profiling;

public class AutoSpeed : MonoBehaviour
{
    [SerializeField]
    Toggle toggle;

    [SerializeField]
    bool autoSpeed = false;

    [SerializeField]
    SimulationTime time;

    const float timesteps = 0.1f;
    
    int timeMultiplier;
    int maxMultiplier;
    const int maxTimeScale = 100;
    int baseMultiplier;

    FPSCounter fpsCounter;

    public float slowDownAt = float.PositiveInfinity;
    public bool fastForward = false;

    const float updatePeriod = 0.2f;
    float nextUpdate = 0;
    const float minFps = 50;
    const float maxFps = 70;

    bool pause = false;


    ProfilerRecorder _totalReservedMemoryRecorder;


    private void Awake()
    {
        fpsCounter = FindObjectOfType<FPSCounter>();
        time = FindObjectOfType<SimulationTime>();
    }

    void OnEnable()
    {
        _totalReservedMemoryRecorder = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "Total Reserved Memory");
    }

    void OnDisable()
    {
        _totalReservedMemoryRecorder.Dispose();
    }

    void Start()
    {
        toggle.isOn = autoSpeed;

        baseMultiplier = Mathf.CeilToInt(1 / timesteps);
        maxMultiplier = baseMultiplier * maxTimeScale;

        ResetTimeMultiplier();
    }

    bool gcCollected = false;

    private void Update()
    {
        if (!autoSpeed || pause || Time.realtimeSinceStartup < nextUpdate) return;

        nextUpdate = Time.realtimeSinceStartup + updatePeriod;

        // If fps is to low
        if (!fastForward && fpsCounter.FPS < minFps && timeMultiplier > 10 && fpsCounter.FPS > 0)
        {
            timeMultiplier = Mathf.Max(10, Mathf.FloorToInt(timeMultiplier - Mathf.Pow(minFps - fpsCounter.FPS, 2) ));
            Time.timeScale = timeMultiplier * timesteps;
            return;
        }

        var usedMemoryGb = _totalReservedMemoryRecorder.LastValue / (1024 * 1024 * 1024);

        // if getting closer to spawn event or memory bound (> 5 GB)
        if (
            (slowDownAt > time.time && // slow down
            Mathf.Ceil(slowDownAt - time.time) <= Mathf.Ceil(Time.deltaTime * timeMultiplier) && // near a slow down
            timeMultiplier > baseMultiplier) ||  // min reached
            (usedMemoryGb > 5 && timeMultiplier > 1 && !Application.isEditor) // memory bound
        )
        {
            if (!gcCollected)
            {
                GC.Collect();
                gcCollected = true;
            }
            timeMultiplier--;
            Time.timeScale = timeMultiplier * timesteps;
            return;
        }
        
        // Use free fps to speed up the simulation, if memory < 4 GB
        if (timeMultiplier < maxMultiplier && (fpsCounter.FPS > maxFps || fastForward) && (usedMemoryGb < 4 || Application.isEditor))
        {
            timeMultiplier = Mathf.Min(maxMultiplier, timeMultiplier + Mathf.FloorToInt(Mathf.Sqrt(fpsCounter.FPS - maxFps)));
            Time.timeScale = timeMultiplier * timesteps;
            gcCollected = false;
            return;
        }

    }

    public void SetSpeedPercentage(float percentage)
    {
        autoSpeed = false;
        toggle.isOn = false;
        timeMultiplier = Mathf.CeilToInt(maxMultiplier * percentage);
        Time.timeScale = timeMultiplier * timesteps;
    }

    public void SetAutospeed(bool value)
    {
        autoSpeed = value;

        if (!value)
        {
            ResetTimeMultiplier();
        }

        toggle.isOn = value;
    }

    public void SetTimeMultiplier(int value)
    {
        timeMultiplier = value < 0 ? 0 : (value > maxMultiplier ? maxMultiplier : value);
        Time.timeScale = timeMultiplier * timesteps;
    }

    public void Pause()
    {
        Time.timeScale = 0;
        pause = true;
    }

    public void Resume()
    {
        pause = false;
        Time.timeScale = timeMultiplier * timesteps;
    }

    public void Increase(int factor = 1)
    {
        timeMultiplier += factor;
        timeMultiplier = timeMultiplier > maxMultiplier ? maxMultiplier : timeMultiplier;
        Time.timeScale = timeMultiplier * timesteps;
        autoSpeed = false;
        toggle.isOn = false;
    }

    public void Decrease(int factor = 1)
    {
        timeMultiplier -= factor;
        timeMultiplier = timeMultiplier < 0 ? 0 : timeMultiplier;
        Time.timeScale = timeMultiplier * timesteps;
        autoSpeed = false;
        toggle.isOn = false;
    }

    public void UpdatedTimeScale()
    {
        timeMultiplier = Mathf.FloorToInt(Time.timeScale / timesteps);
        autoSpeed = false;
        toggle.isOn = false;
    }

    public void Reset() => ResetTimeMultiplier();

    void ResetTimeMultiplier()
    {
        timeMultiplier = Mathf.CeilToInt(1f / timesteps);
    }

    public void Enable()
    {
        autoSpeed = true;
        toggle.isOn = true;
    }

    internal void Disable()
    {
        autoSpeed = false;
        toggle.isOn = false;
    }
}
