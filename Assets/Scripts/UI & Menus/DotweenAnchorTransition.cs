using UnityEngine;
using DG.Tweening;
using UnityEngine.Events; 

public class DotweenAnchorTransition : MonoBehaviour
{
    [SerializeField] RectTransform rectTransform;
    [SerializeField] float outPos = 0;
    [SerializeField] float inPos = 0;
    [SerializeField] float transitionDuration;
    [SerializeField] bool doYPosTransition = true;
    [SerializeField] bool doXPosTransition;
    [SerializeField] bool introOnEnable = false;
    public UnityEvent OnInTransitionComplete;
    public UnityEvent OnInTransitionStarted;

    public UnityEvent OnOutTransitionComplete;
    public UnityEvent OnOutTransitionStarted;

    private void Awake()
    {
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        SetToOutPosition();
    }

    private void OnEnable()
    {
        if(introOnEnable)
            DoIntro();
    }

    private void OnDisable()
    {
        rectTransform.DOComplete();
        SetToOutPosition();
    }
    private void SetToOutPosition()
    {
        if (doYPosTransition)
        {
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, outPos);
        }
        else if (doXPosTransition)
        {
            rectTransform.anchoredPosition = new Vector2(outPos, rectTransform.anchoredPosition.y);
        }
    }

    public void DoIntro()
    {
        if (doYPosTransition)
        {
            OnInTransitionStarted?.Invoke();
            rectTransform.DOAnchorPosY(inPos, transitionDuration).OnComplete(() =>
            {
                OnInTransitionComplete?.Invoke();
            });
        }
        else if (doXPosTransition)
        {
            OnInTransitionStarted?.Invoke();
            rectTransform.DOAnchorPosX(inPos, transitionDuration).OnComplete(() =>
            {
                OnInTransitionComplete?.Invoke();
            });
        }

        //Debug.Log("Did intro");
    }

    public void DoOutro()
    {
        if (doYPosTransition)
        {
            OnOutTransitionStarted?.Invoke();
            rectTransform.DOAnchorPosY(outPos, transitionDuration).OnComplete(() =>
            {
                OnOutTransitionComplete?.Invoke();
            });
        }

        else if (doXPosTransition)
        {
            OnOutTransitionStarted?.Invoke();
            rectTransform.DOAnchorPosX(outPos, transitionDuration).OnComplete(() =>
            {
                OnOutTransitionComplete?.Invoke();
            });
        }
    }
}