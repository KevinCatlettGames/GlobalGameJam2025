using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class UninteractableOnDemo : MonoBehaviour
{
    public bool turnOffIfToggle = true; 

    private void OnEnable()
    {
        if (SteamIntegration.instance != null)
        {
            if (!SteamIntegration.instance.IsFullVersion)
            {
                Selectable uiElement = GetComponent<Selectable>();

                if (uiElement != null)
                {
                    uiElement.interactable = false;

                    if (uiElement is Toggle toggleElement && turnOffIfToggle)
                    {
                        toggleElement.isOn = false;
                    }
                }
                else
                {
                    Debug.LogWarning($"No Selectable UI component found on {gameObject.name}", gameObject);
                }
            }
        }
    }
}