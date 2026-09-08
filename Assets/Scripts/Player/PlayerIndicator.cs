using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerIndicator : MonoBehaviour
{
    [SerializeField] private GameObject arrowObject;
    [SerializeField] private GameObject iconObject;
    [SerializeField] private GameObject textObject;
    [SerializeField] private Image arrowImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI text;

    private bool isUsingImage = false;

    public void InitialiseIndicator(Color color, int playerID)
    {
        arrowImage.color = color;
        text.color = color;
        text.text = "P" + (playerID + 1);
    }

    public void InitialiseIndicator(Color color, Sprite icon)
    {
        isUsingImage = true;
        iconImage.sprite = icon;
        arrowImage.color = color;
        text.color = color;
    }

    public void ToggleIndicator(bool enabled)
    {
        arrowObject.SetActive(enabled);
        if (isUsingImage)
            iconObject.SetActive(enabled);
        else
            textObject.SetActive(enabled);
    }
}
