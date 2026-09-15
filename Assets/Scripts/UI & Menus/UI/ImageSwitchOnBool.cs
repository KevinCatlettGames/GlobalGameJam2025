using UnityEngine;
using UnityEngine.UI; 

public class ImageSwitchOnBool : MonoBehaviour
{
    [SerializeField] private Sprite trueImage;
    [SerializeField] private Sprite falseImage;
    [SerializeField] private Image imageToSet;
    [SerializeField] private bool setSecondaryImageActive = false;
    [SerializeField] private GameObject secondaryImage; 

    public void SetImage(bool trigger)
    {
        if (trigger)
        {
            imageToSet.sprite = trueImage;
            if (secondaryImage && setSecondaryImageActive)
                secondaryImage.SetActive(false);
        }
        else
        {
            imageToSet.sprite = falseImage;

            if (secondaryImage && setSecondaryImageActive)
                secondaryImage.SetActive(true);
        }
    }
}