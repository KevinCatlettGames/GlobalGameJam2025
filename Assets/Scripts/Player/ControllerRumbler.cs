using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerRumbler : MonoBehaviour
{
    private Gamepad controller;
    [SerializeField] private float lowFrFactor = .2f;
    private float lowMotorSpeed = 0f;
    [SerializeField] private float highFrFactor = .5f;
    private float highMotorSpeed = 0f;
    private float timer = 0f;
    [SerializeField] private float maxDuration = .7f;
    public void SetController(Gamepad gamepad)
    {
        controller = gamepad;
    }
    void Update()
    {
        if (controller != null && timer > 0) 
        {
            timer -= Time.deltaTime;
            if (timer <= 0)
            {
                timer = 0;
                controller.SetMotorSpeeds(0, 0);
            }
        }
    }
    public void Rumble(float duration, float lowIntensity, float highIntensity)
    {
        timer = duration;
        if (timer > maxDuration) timer = maxDuration;

#if UNITY_SWITCH
        lowMotorSpeed = (lowIntensity *.5f) * lowFrFactor;
        highMotorSpeed = (highIntensity * .5f) * highFrFactor;
#else
        lowMotorSpeed = lowIntensity * lowFrFactor;
        highMotorSpeed = highIntensity * highFrFactor;
#endif
        if (timer > 0 && controller != null)
        {
            controller.SetMotorSpeeds(lowMotorSpeed, highMotorSpeed);
        }
    }
    private void OnDestroy()
    {
        controller?.SetMotorSpeeds(0, 0);
    }
    private void OnApplicationQuit()
    {
        controller?.SetMotorSpeeds(0, 0);
    }
}
