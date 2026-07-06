using UnityEngine;
using UnityEngine.UI;

public class UninteractableOnDemo : MonoBehaviour
{
    void Start()
    {
        if (SteamIntegration.instance != null)
        {
            if (!SteamIntegration.instance.IsFullVersion)
            {
                Selectable uiElement = GetComponent<Selectable>();

                if (uiElement != null)
                {
                    uiElement.interactable = false;
                }
                else
                {
                    Debug.LogWarning($"No Selectable UI component found on {gameObject.name}", gameObject);
                }
            }
        }
    }
}