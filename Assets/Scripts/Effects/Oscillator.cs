using UnityEngine;

/// <summary>
/// This class oscillates the GameObject along a specified direction with a given amplitude and frequency.
/// </summary>
public class Oscillator : MonoBehaviour
{
    [Tooltip("Direction of oscillation.")]
    public Vector3 direction = Vector3.up;

    [Tooltip("Amplitude of the oscillation.")]
    public float amplitude = 0.5f;

    [Tooltip("Frequency of the oscillation.")]
    public float frequency = 1f;

    [Tooltip("Use unscaled time (useful for UI animations when Time.timeScale == 0).")]
    public bool useUnscaledTime = false;

    private Vector3 initialLocalPos;

    void Start()
    {
        initialLocalPos = transform.localPosition;
    }

    void Update()
    {
        float time = useUnscaledTime ? Time.unscaledTime : Time.time;
        float offset = Mathf.Sin(time * frequency) * amplitude;
        transform.localPosition = initialLocalPos + direction * offset;
    }
}