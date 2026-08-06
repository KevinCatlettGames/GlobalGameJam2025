using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UIOscillator : MonoBehaviour
{
    [Tooltip("Direction of oscillation.")]
    public Vector2 direction = Vector2.up;

    [Tooltip("Amplitude of the oscillation.")]
    public float amplitude = 10f;

    [Tooltip("Frequency of the oscillation.")]
    public float frequency = 1f;

    [Tooltip("Use unscaled time (recommended for UI animations).")]
    public bool useUnscaledTime = true;

    private RectTransform rectTransform;
    private Vector2 initialAnchoredPos;
    private float startTime;

    void OnEnable()
    {
        startTime = GetCurrentTime();
        rectTransform = GetComponent<RectTransform>();
        initialAnchoredPos = rectTransform.anchoredPosition;
    }

    void OnDisable()
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = initialAnchoredPos;
        }
    }

    void Update()
    {
        float elapsedTime = GetCurrentTime() - startTime;
        float offset = Mathf.Sin(elapsedTime * frequency) * amplitude;

        rectTransform.anchoredPosition =
            initialAnchoredPos + direction.normalized * offset;
    }

    private float GetCurrentTime()
    {
        return useUnscaledTime ? Time.unscaledTime : Time.time;
    }
}