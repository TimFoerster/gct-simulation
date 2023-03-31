using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TextMeshProUGUI))]
public class FPSCounter : MonoBehaviour
{
    private float m_CurrentFps;
    const string display = "{0:00} FPS";
    private TextMeshProUGUI m_Text;
    float nextUiUpdate;
    const float uiUpdateInterval = .1f;

    const float fpsMeasurePeriod = 0.2f;
    private int m_FpsAccumulator = 0;
    private float m_FpsNextPeriod = 0;

    private void Start()
    {
        m_Text = GetComponent<TextMeshProUGUI>();
    }

    private void Update()
    {
        m_FpsAccumulator++;
        if (Time.realtimeSinceStartup > m_FpsNextPeriod)
        {
            m_CurrentFps = (float)m_FpsAccumulator / fpsMeasurePeriod;
            m_FpsAccumulator = 0;
            m_FpsNextPeriod += fpsMeasurePeriod;
        }
    }

    private void LateUpdate()
    {
        if (Time.realtimeSinceStartup > nextUiUpdate)
        {
            m_Text.text = string.Format(display, m_CurrentFps);
            nextUiUpdate += uiUpdateInterval;
        }
    }

    public float FPS => m_CurrentFps;
}
