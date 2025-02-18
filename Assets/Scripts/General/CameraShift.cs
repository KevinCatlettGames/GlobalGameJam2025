using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

public class CameraShift : MonoBehaviour
{
    [SerializeField] GameObject startCam;
    [SerializeField] GameObject endCam;
    [SerializeField] float shiftDelay = 1.0f;
    void Start()
    {
        StartCoroutine(ShiftDelay());
    }

    private IEnumerator ShiftDelay()
    {
        yield return new WaitForSeconds(shiftDelay);
        startCam.SetActive(false);
        endCam.SetActive(true);
    }
}
