using UnityEngine;
using UnityEngine.UI;

public class PlayerStatusIndicator : MonoBehaviour
{
    private Image image;

    [SerializeField] private Sprite[] statusSprites;
    [SerializeField] private Color[] statusColores;

    private void Start()
    {
        image = GetComponent<Image>();
    }
    public void SetStatus(ShaderState status)
    {
        int i = ((int)status);
        if (i == 0)
        {
            image.enabled = false;
        }
        else
        {
            image.enabled = true;
            image.sprite = statusSprites[i];
            image.color = statusColores[i];
        }
    }
}
