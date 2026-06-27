using UnityEngine;

public class fpsLock : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
#if UNITY_SWITCH
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 30;
#endif
    }
}