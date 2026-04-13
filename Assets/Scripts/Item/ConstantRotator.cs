using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstantRotator : MonoBehaviour
{
    [SerializeField] private float rotationSpeed = 50f; // Speed of rotation in degrees per second
    [SerializeField] private Vector3 rotationAxis = Vector3.up;
    void Update()
    {
        // Rotate the object around its Y-axis (world space), can be modified to other axes
        transform.Rotate(rotationAxis * (rotationSpeed * Time.deltaTime));
    }
}
