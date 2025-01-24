using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ConstantRotator : MonoBehaviour
{
    public float rotationSpeed = 50f; // Speed of rotation in degrees per second

    void Update()
    {
        // Rotate the object around its Y-axis (world space), can be modified to other axes
        transform.Rotate(Vector3.up * (rotationSpeed * Time.deltaTime));
    }
}
