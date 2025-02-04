using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraShift : MonoBehaviour
{
    [SerializeField] GameObject startCam;
    [SerializeField] GameObject endCam;
    void Start()
    {
        startCam.SetActive(false);
        endCam.SetActive(true);
    }

}
