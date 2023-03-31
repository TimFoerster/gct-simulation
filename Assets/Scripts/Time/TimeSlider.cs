using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimeSlider : MonoBehaviour
{
    Slider slider;
    AutoSpeed autoSpeed;

    public float timeSpan = 0.3f;
    private float time;

    public bool valueUpdate;

    // Start is called before the first frame update
    void Awake()
    {
        slider = GetComponent<Slider>();
        autoSpeed = FindObjectOfType<AutoSpeed>();
    }

    void Update()
    {

        // plus key pressed
        if (Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            autoSpeed.Increase();
        }

        // - key pressed
        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            autoSpeed.Decrease();
        }

        // key pressed?
        int num = GetPressedNumber();
        if (num == 0)
        {
            autoSpeed.Pause();
        }
        else if (num == 1)
        {
            autoSpeed.Reset();
            autoSpeed.Resume();
        }
        else if (num > 1)
        {
            autoSpeed.SetSpeedPercentage((float)(num - 1) / 8f);
        } 
        else
        // plus key hold down?
        if (Input.GetKey(KeyCode.Plus) || Input.GetKey(KeyCode.KeypadPlus))
        {
            // wait threashold
            if (time < timeSpan)
            {
                time += Time.unscaledDeltaTime;
                return;
            }
            autoSpeed.Increase();

        }
        else 
        // minus key hold down?
        if (Input.GetKey(KeyCode.Minus) || Input.GetKey(KeyCode.KeypadMinus))
        {
            // wait threashold
            if (time < timeSpan)
            {
                time += Time.unscaledDeltaTime;
                return;
            }
            autoSpeed.Decrease();
        }
        else
        // key reset?
        if (Input.GetKeyUp(KeyCode.Minus) || Input.GetKeyUp(KeyCode.KeypadMinus) || 
        Input.GetKeyUp(KeyCode.Plus) || Input.GetKeyUp(KeyCode.KeypadPlus))
        {
            time = 0;
        }
        // nothing happend?
        else
        {
            slider.value = Time.timeScale;
            return;
        }

        slider.value = Time.timeScale;
        autoSpeed.UpdatedTimeScale();
    }

    public void SetTime(float value)
    {
        Time.timeScale = value;
    }

    int GetPressedNumber()
    {
        if (Input.GetKeyDown(KeyCode.Alpha0) || Input.GetKeyDown(KeyCode.Keypad0))
            return 0;

        if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
            return 1;
        
        if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
            return 2;
        
        if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3))
            return 3;
        
        if (Input.GetKeyDown(KeyCode.Alpha4) || Input.GetKeyDown(KeyCode.Keypad4))
            return 4;
        
        if (Input.GetKeyDown(KeyCode.Alpha5) || Input.GetKeyDown(KeyCode.Keypad5))
            return 5;
        
        if (Input.GetKeyDown(KeyCode.Alpha6) || Input.GetKeyDown(KeyCode.Keypad6))
            return 6;

        if (Input.GetKeyDown(KeyCode.Alpha7) || Input.GetKeyDown(KeyCode.Keypad7))
            return 7;

        if (Input.GetKeyDown(KeyCode.Alpha8) || Input.GetKeyDown(KeyCode.Keypad8))
            return 8;

        if (Input.GetKeyDown(KeyCode.Alpha9) || Input.GetKeyDown(KeyCode.Keypad9))
            return 9;


        return -1;
    }
}
