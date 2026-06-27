using UnityEngine;
using UnityEngine.UI; 

public class ChangeImageDeviceDependent : MonoBehaviour
{
    [SerializeField] Sprite switchImage;
    [SerializeField] Color switchColor;
    [SerializeField] Image imageToChange;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
#if UNITY_SWITCH
        imageToChange.sprite = switchImage;
        imageToChange.color = switchColor;
#endif 
    }
}
