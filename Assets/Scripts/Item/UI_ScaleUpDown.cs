using UnityEngine;

public class UI_ScaleUpDown : MonoBehaviour
{
    private RectTransform rectTransform;
    [SerializeField] private float minSize = 1f;
    [SerializeField] private float maxSize = 2f;
    [SerializeField] private float scaleSpeed = 1f;
    [SerializeField] private bool isScaleing = true;
    [SerializeField] private bool isGrowing = true;

    public bool IsScaleing => isScaleing;

    private float currentSize = 1f;
    private Vector3 localScale;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        localScale = rectTransform.localScale;
    }

    private void Update()
    {
        if (!isScaleing) return;
        if (isGrowing)
        {
            if (currentSize >= maxSize)
            {
                currentSize = maxSize;
                isGrowing = false;
            }
            else
            {
                currentSize += scaleSpeed * Time.unscaledDeltaTime;
            }
        }
        else
        {
            if (currentSize <= minSize)
            {
                currentSize = minSize;
                isGrowing = true;
            }
            else
            {
                currentSize -= scaleSpeed * Time.unscaledDeltaTime;
            }
        }
        rectTransform.localScale = localScale * currentSize;
    }
}
