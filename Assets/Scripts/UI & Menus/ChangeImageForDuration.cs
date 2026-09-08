using UnityEngine;
using UnityEngine.UI;
using System.Collections; 

public class ChangeImageForDuration : MonoBehaviour
{
    [SerializeField] private Image image;
    [SerializeField] private bool alsoChangeColor;
    [SerializeField] private Color newColor;
    [SerializeField] private Sprite newSprite;
    private Sprite originalSprite;
    private Color originalColor;
    [SerializeField] private float duration = .5f;

    private void Awake()
    {
        if (image == null && GetComponent<Image>())
        {
            image = GetComponent<Image>();
            originalSprite = image.sprite;
            originalColor = image.color;
        }
    }

    public void DoIt()
    {
        if (!image) return;

        StopCoroutine(ChangeImage());
        StartCoroutine(ChangeImage());
    }

    IEnumerator ChangeImage()
    {
        image.sprite = newSprite;
        if(alsoChangeColor)
            image.color = newColor;
        yield return new WaitForSeconds(duration);
        image.sprite = originalSprite;
        image.color = originalColor;
    }

    private void OnDisable()
    {
        if (image == null) return;
        image.sprite = originalSprite;
        image.color = originalColor;
    }

}