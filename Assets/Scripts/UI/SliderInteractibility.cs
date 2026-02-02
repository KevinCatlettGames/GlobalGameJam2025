using UnityEngine;
using UnityEngine.UI;

public class SliderInteractibility : MonoBehaviour
{
    [SerializeField] private Slider slider;

    public void SetAsBool(bool toggle)
    {
        slider.interactable = toggle;
    }

    public void SetOppositeOfBool(bool toggle)
    {
        slider.interactable = !toggle;
    }
}
