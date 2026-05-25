using UnityEngine;

public class SkyboxRotator : MonoBehaviour
{
    // Rotation speed in degrees per second
    public float rotationSpeed = 10f;

    void Update()
    {
        // Calculate the new rotation angle
        float rotation = Time.time * rotationSpeed;
      
        // Apply the rotation to the skybox's "_Rotation" property
        RenderSettings.skybox.SetFloat("_Rotation", rotation);
    }
}