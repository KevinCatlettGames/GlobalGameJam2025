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
        // Assign Image component if not set in the Inspector
        if (image == null && GetComponent<Image>())
            image = GetComponent<Image>();
        
        // Delay color change slightly after startup
        if (image)
            Invoke(nameof(ChangeColor), 0.2f);
    }

    void ChangeColor()
    {
        // Check which object is active and update the image color
        for (int i = 0; i < objectsToCheck.Length; i++)
        {
            if (objectsToCheck[i].activeSelf)
            {
                image.color = colors[i];
                break; // Stop after finding the first active object
            }
        }
    }
}