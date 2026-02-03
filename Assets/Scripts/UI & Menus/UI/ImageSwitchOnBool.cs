using UnityEngine;
using UnityEngine.UI; 

public class ImageSwitchOnBool : MonoBehaviour
{
    [SerializeField] private Sprite trueImage;
    [SerializeField] private Sprite falseImage;
    [SerializeField] private Image imageToSet;
    
    public void SetImage(bool trigger)
    {
        if(trigger)
            imageToSet.sprite = trueImage;
        else
            imageToSet.sprite = falseImage;
    }
}