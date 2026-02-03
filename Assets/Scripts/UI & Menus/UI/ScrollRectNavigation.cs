using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ScrollRectNavigation : MonoBehaviour
{
    public ScrollRect scrollRect;
    public float scrollSpeed = 10f;
    public float verticalOffset = 0f; // positive = move selection up, negative = move down

    void Update()
    {
        if (EventSystem.current.currentSelectedGameObject == null) return;

        RectTransform selected = EventSystem.current.currentSelectedGameObject.GetComponent<RectTransform>();
        RectTransform content = scrollRect.content;
        RectTransform viewport = scrollRect.viewport;

        Vector3 selectedWorldPos = selected.position;
        Vector3 viewportLocalPos = viewport.InverseTransformPoint(selectedWorldPos);

        // Apply vertical offset
        float offsetY = viewportLocalPos.y - verticalOffset;

        Vector3 targetPos = content.localPosition - new Vector3(0, offsetY, 0);

        // Clamp target position
        float contentHeight = content.rect.height;
        float viewportHeight = viewport.rect.height;

        float minY = 0;
        float maxY = Mathf.Max(0, contentHeight - viewportHeight);
        targetPos.y = Mathf.Clamp(targetPos.y, minY, maxY);

        // Smoothly move
        content.localPosition = Vector3.Lerp(content.localPosition, targetPos, Time.deltaTime * scrollSpeed);
    }
}