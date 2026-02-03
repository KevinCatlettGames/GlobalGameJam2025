using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SliderTextChangeToValue : MonoBehaviour
{
    [SerializeField] private Slider slider; 
    [SerializeField] private TextMeshProUGUI text; 
    
    public void SetText()
    {
        text.text = slider.value.ToString();
    }
}