using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TextMeshProUGUI))]
public class GameTime : MonoBehaviour
{

    TMP_Text text;

    [SerializeField]
    SimulationTime time;

    float nextUpdate = 0;
    float offset = 0;

    public float Offset { get => offset; set => offset = value; }

    // Start is called before the first frame update
    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (nextUpdate <= Time.time)
        {
            nextUpdate += 1;
            text.text = TimeString;
        }
    }

    public string TimeString => System.TimeSpan.FromSeconds(time.time + offset).ToString(@"hh\:mm\:ss\.ff");




}
