using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstantRotator : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 50f;
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    void Update()
    {
        transform.Rotate(rotationAxis * (rotationSpeed * Time.unscaledDeltaTime));
    }
}