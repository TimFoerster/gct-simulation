using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayHandler : MonoBehaviour
{

    AutoSpeed autoSpeed;

    // Start is called before the first frame update
    void Start()
    {
        autoSpeed = FindObjectOfType<AutoSpeed>();
    }


    public void OnPlay()
    {
        autoSpeed.Reset();
        autoSpeed.Resume();
    }

}
