using UnityEngine;

public class SkyboxRotator : MonoBehaviour
{
    [Header("Rotation Speeds (Degrees/Sec)")]
    public float pitchSpeed = 0f;
    public float yawSpeed = 10f;
    public float rollSpeed = 0f;

    private float currentPitch;
    private float currentYaw;
    private float currentRoll;

    private float initialPitch;
    private float initialYaw;
    private float initialRoll;

    private static readonly int RotationXProperty = Shader.PropertyToID("_RotationX");
    private static readonly int RotationYProperty = Shader.PropertyToID("_RotationY");
    private static readonly int RotationZProperty = Shader.PropertyToID("_RotationZ");

    private void OnEnable()
    {
        if (RenderSettings.skybox == null) return;

        initialPitch = RenderSettings.skybox.HasProperty(RotationXProperty) ? RenderSettings.skybox.GetFloat(RotationXProperty) : 0f;
        initialYaw = RenderSettings.skybox.HasProperty(RotationYProperty) ? RenderSettings.skybox.GetFloat(RotationYProperty) : 0f;
        initialRoll = RenderSettings.skybox.HasProperty(RotationZProperty) ? RenderSettings.skybox.GetFloat(RotationZProperty) : 0f;

        currentPitch = initialPitch;
        currentYaw = initialYaw;
        currentRoll = initialRoll;
    }

    private void Update()
    {
        if (RenderSettings.skybox == null) return;

        if (pitchSpeed != 0f) currentPitch += pitchSpeed * Time.deltaTime;
        if (yawSpeed != 0f) currentYaw += yawSpeed * Time.deltaTime;
        if (rollSpeed != 0f) currentRoll += rollSpeed * Time.deltaTime;

        ApplyRotation(currentPitch, currentYaw, currentRoll);
    }

    private void OnDisable()
    {
        ResetToInitial();
    }

    private void OnApplicationQuit()
    {
        ResetToInitial();
    }

    private void ResetToInitial()
    {
        if (RenderSettings.skybox == null) return;
        ApplyRotation(initialPitch, initialYaw, initialRoll);
    }

    private void ApplyRotation(float pitch, float yaw, float roll)
    {
        if (RenderSettings.skybox.HasProperty(RotationXProperty)) RenderSettings.skybox.SetFloat(RotationXProperty, pitch);
        if (RenderSettings.skybox.HasProperty(RotationYProperty)) RenderSettings.skybox.SetFloat(RotationYProperty, yaw);
        if (RenderSettings.skybox.HasProperty(RotationZProperty)) RenderSettings.skybox.SetFloat(RotationZProperty, roll);
    }
}