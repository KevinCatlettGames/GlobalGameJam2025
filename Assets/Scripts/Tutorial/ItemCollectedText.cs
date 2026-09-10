using UnityEngine;

public class ItemCollectedText : MonoBehaviour
{
    [SerializeField] Item[] items;
    [SerializeField] private GameObject canvas;
    private bool isActive = false;

    private void Start()
    {
        foreach (Item item in items)
        {
            item.OnCollected += EnableText;
        }
    }

    private void EnableText()
    {
        if (isActive)
            return;
        isActive = true;
        canvas.SetActive(true);
        foreach (Item item in items)
        {
            item.OnCollected -= EnableText;
        }
    }

    private void OnDestroy()
    {
        if (isActive)
            return;
        foreach (Item item in items)
        {
            item.OnCollected -= EnableText;
        }
    }
}
