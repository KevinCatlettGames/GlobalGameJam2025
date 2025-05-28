using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    public static CameraShaker instance;
    public CinemachineVirtualCamera cam;
    private CinemachineBasicMultiChannelPerlin shakePerlin;
    private float shakeTimer;
    /// <summary>
    /// Declares this as a singelton
    /// </summary>
    private void Start()
    {
        if (!CameraShaker.instance)
        {
            instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
        cam = GetComponent<CinemachineVirtualCamera>();
        shakePerlin = cam.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
    }
    /// <summary>
    /// This methods is called multiple times and starts to shake the camera 
    /// </summary>
    /// <param name="time">time the camera is shaked</param>
    /// <param name="intesity">the intensity with witch it is shaked</param>
    public void ShakeCamera(float time, float intesity)
    {
        shakeTimer = time;
        shakePerlin.m_AmplitudeGain = intesity;
    }
    /// <summary>
    /// Counts down the timer and stops the shaking if it reaches zero
    /// </summary>
    private void Update()
    {
        if (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
        }
        else
        {
            shakePerlin.m_AmplitudeGain = 0;
        }
    }
}
