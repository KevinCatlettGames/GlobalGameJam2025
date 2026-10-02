using System.Collections;
using UnityEngine;

public class ScaleToCorrectSize : MonoBehaviour
{
    [Header("Scale Settings")]
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private float startScaleMultiplier = 0f;
    [SerializeField]
    private AnimationCurve scaleCurve =
        AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 targetScale;
    private Coroutine scaleRoutine;
    public bool scaleOnEnable = false; 

    private void Awake()
    {
        targetScale = transform.localScale;
    }

    private void OnEnable()
    {
        if (scaleOnEnable)
            Play();
    }

    public void Play()
    {
        if (scaleRoutine != null)
        {
            StopCoroutine(scaleRoutine);
            transform.localScale = targetScale;
        }

        scaleRoutine = StartCoroutine(ScaleRoutine());
    }

    private IEnumerator ScaleRoutine()
    {
        float timer = 0f;

        Vector3 startScale = targetScale * startScaleMultiplier;
        transform.localScale = startScale;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(timer / duration);

            float curveValue = scaleCurve.Evaluate(t);

            transform.localScale =
                Vector3.LerpUnclamped(startScale, targetScale, curveValue);

            yield return null;
        }

        transform.localScale = targetScale;

        scaleRoutine = null;
    }

    private void OnDisable()
    {
        transform.localScale = targetScale;
    }
}