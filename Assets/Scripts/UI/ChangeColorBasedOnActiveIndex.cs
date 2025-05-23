using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ChangeColorBasedOnActiveIndex : MonoBehaviour
{
    [SerializeField] private Image image; 
    [SerializeField] private GameObject[] objectsToCheck;
    [SerializeField] private Color[] colors;

    private void Awake()
    {
        if (image == null && GetComponent<Image>())
            image = GetComponent<Image>();
        
        if (image)
            Invoke(nameof(ChangeColor), 0.2f);
    }

    void ChangeColor()
    {
        for (int i = 0; i < objectsToCheck.Length; i++)
        {
            if (objectsToCheck[i].activeSelf)
            {
                image.color = colors[i];
                break;
            }
        }
    }
}