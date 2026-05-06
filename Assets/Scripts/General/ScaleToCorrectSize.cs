using System.Collections;
using UnityEngine;

public class ScaleToCorrectSize : MonoBehaviour
{
    [Header("Scale Settings")]
    [SerializeField] private float duration = 0.3f;
    [SerializeField] private float startScaleMultiplier = 0f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Vector3 targetScale;
    private Coroutine scaleRoutine;

    private void Awake()
    {
        targetScale = transform.localScale;
    }

    public void Play()
    {
        if (scaleRoutine != null)
            StopCoroutine(scaleRoutine);

        scaleRoutine = StartCoroutine(ScaleRoutine());
    }

    private IEnumerator ScaleRoutine()
    {
        float timer = 0f;

        Vector3 startScale = targetScale * startScaleMultiplier;
        transform.localScale = startScale;

        while (timer < duration)
        {
            timer += Time.deltaTime;
            float t = timer / duration;

            float curveValue = scaleCurve.Evaluate(t);
            transform.localScale = Vector3.LerpUnclamped(startScale, targetScale, curveValue);

            yield return null;
        }

        transform.localScale = targetScale;
    }
}