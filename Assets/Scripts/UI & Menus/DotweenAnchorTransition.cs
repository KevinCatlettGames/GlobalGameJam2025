using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

public class DotweenAnchorTransition : MonoBehaviour
{
    [Header("Components")]
    [SerializeField] private RectTransform rectTransform;

    [Header("Positions")]
    [SerializeField] private float outPos = 0f;
    [SerializeField] private float inPos = 0f;
    [SerializeField] private float transitionDuration = 0.3f;

    [Header("Transition Axis")]
    [SerializeField] private bool doYPosTransition = true;
    [SerializeField] private bool doXPosTransition;

    [Header("Settings")]
    [SerializeField] private bool introOnEnable = false;
    [SerializeField] private Ease easeIn = Ease.OutCubic;
    [SerializeField] private Ease easeOut = Ease.InCubic;

    [Header("Events")]
    public UnityEvent OnInTransitionStarted;
    public UnityEvent OnInTransitionComplete;
    public UnityEvent OnOutTransitionStarted;
    public UnityEvent OnOutTransitionComplete;

    private void Awake()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        SetToOutPosition();
    }

    private void OnEnable()
    {
        if (introOnEnable)
            DoIntro();
    }

    private void OnDisable()
    {
        // Stop any active tweens on this transform to avoid memory leaks or ghost callbacks
        rectTransform.DOKill();
        SetToOutPosition();
    }

    private void SetToOutPosition()
    {
        if (doYPosTransition)
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, outPos);
        else if (doXPosTransition)
            rectTransform.anchoredPosition = new Vector2(outPos, rectTransform.anchoredPosition.y);
    }

    public void DoIntro()
    {
        // Kill existing tween so fast-toggling doesn't glitch position
        rectTransform.DOKill();
        OnInTransitionStarted?.Invoke();

        Tween tween = GetTween(inPos);

        tween?.SetEase(easeIn)
               .SetUpdate(true) // Unscaled time (works when paused)
               .OnComplete(() => OnInTransitionComplete?.Invoke());
    }

    public void DoOutro()
    {
        rectTransform.DOKill();
        OnOutTransitionStarted?.Invoke();

        Tween tween = GetTween(outPos);

        tween?.SetEase(easeOut)
               .SetUpdate(true)
               .OnComplete(() => OnOutTransitionComplete?.Invoke());
    }

    // Helper method to keep intro/outro logic DRY
    private Tween GetTween(float targetPos)
    {
        if (doYPosTransition)
            return rectTransform.DOAnchorPosY(targetPos, transitionDuration);
        if (doXPosTransition)
            return rectTransform.DOAnchorPosX(targetPos, transitionDuration);

        return null;
    }
}