using System.Collections;
using UnityEngine;
using FMOD.Studio;
using FMODUnity;

public class PointBubbleResizeEffect : MonoBehaviour
{
    [Header("Scale Effect Settings")]
    [SerializeField] private float appearDuration = 0.3f;
    [SerializeField] private float overshootScale = 1.2f;
    [SerializeField] private float beginEffectWaitTime = 0.5f;
    [SerializeField] private AnimationCurve scaleCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Sound Events")] [SerializeField]
    private StudioEventEmitter emitter;
    
    public bool HasPerformedEffect { get; set; }

    private Vector3 startScale;
    private Coroutine resizeRoutine;

    private void Awake()
    {
        startScale = transform.localScale;
    }

    public void PlayEffect()
    {
        if (HasPerformedEffect) return;

        if (resizeRoutine != null)
            StopCoroutine(resizeRoutine);

        resizeRoutine = StartCoroutine(PlayResizeEffect());
    }

    private IEnumerator PlayResizeEffect()
    {
        transform.localScale = Vector3.zero;
        yield return new WaitForSeconds(beginEffectWaitTime);

        emitter.Play();

        float timer = 0f;
        while (timer < appearDuration)
        {
            timer += Time.deltaTime;
            float t = timer / appearDuration;
            float curveValue = scaleCurve.Evaluate(t);

            float scaleFactor = Mathf.LerpUnclamped(0f, overshootScale, curveValue);
            transform.localScale = startScale * scaleFactor;

            yield return null;
        }

        transform.localScale = startScale;
        HasPerformedEffect = true;
    }
}