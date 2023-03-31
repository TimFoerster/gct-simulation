using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class TimeDisplay : MonoBehaviour
{
    TMP_InputField tmp;
    float ts;

    // Start is called before the first frame update
    void Start()
    {
        tmp = GetComponent<TMP_InputField>();
        ts = Time.timeScale;
    }

    private void LateUpdate()
    {
        if (ts == Time.timeScale) return;

        ts = Time.timeScale;
        tmp.text = ts.ToString();
    }
}
