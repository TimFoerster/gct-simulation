using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PauseHandler : MonoBehaviour
{

    AutoSpeed autoSpeed;
    // Start is called before the first frame update
    void Start()
    {
        autoSpeed = FindObjectOfType<AutoSpeed>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space)) 
            OnPause();
    }

    public void OnPause()
    {
        if (Time.timeScale == 0f)
            autoSpeed.Resume();
        else
            autoSpeed.Pause();
    }
}
