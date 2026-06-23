using UnityEngine;

/// <summary>
/// Oscillates the GameObject's rotation around a specified axis.
/// </summary>
public class RotationOscillator : MonoBehaviour
{
    [Tooltip("Axis of rotation oscillation.")]
    public Vector3 axis = Vector3.forward;

    [Tooltip("Maximum rotation offset in degrees.")]
    public float amplitude = 10f;

    [Tooltip("Oscillation frequency.")]
    public float frequency = 1f;

    [Tooltip("Use unscaled time (useful for UI animations when Time.timeScale == 0).")]
    public bool useUnscaledTime = false;

    private Quaternion initialLocalRotation;

    private void Start()
    {
        initialLocalRotation = transform.localRotation;
    }

    private void Update()
    {
        float time = useUnscaledTime ? Time.unscaledTime : Time.time;
        float angle = Mathf.Sin(time * frequency) * amplitude;

        transform.localRotation =
            initialLocalRotation *
            Quaternion.AngleAxis(angle, axis.normalized);
    }
}