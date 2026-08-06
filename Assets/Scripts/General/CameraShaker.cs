using Cinemachine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShaker : MonoBehaviour
{
    public static CameraShaker instance;
    private CinemachineVirtualCamera cam;
    private CinemachineBasicMultiChannelPerlin shakePerlin;
    private float shakeTimer;
    private Vector3 position;
    private Quaternion rotation;
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
        rotation = transform.rotation;
    }
    /// <summary>
    /// This methods is called multiple times and starts to shake the camera 
    /// </summary>
    /// <param name="time">time the camera is shaked</param>
    /// <param name="intesity">the intensity with witch it is shaked</param>
    public void ShakeCamera(float time, float intesity)
    {
        if (shakeTimer > 0) return;
        shakeTimer = time;
        shakePerlin.m_AmplitudeGain = intesity;
        StartCoroutine(ShakeCameraCoroutine());
    }

    private IEnumerator ShakeCameraCoroutine()
    {
        while (shakeTimer > 0)
        {
            shakeTimer -= Time.deltaTime;
            yield return null;
        }
        shakePerlin.m_AmplitudeGain = 0;
        transform.rotation = rotation;
    }
}
