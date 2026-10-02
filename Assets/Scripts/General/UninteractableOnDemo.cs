using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events; 

public class UninteractableOnDemo : MonoBehaviour
{
    public bool turnOffIfToggle = true;
    public UnityEvent OnInteractionDisabled; 

    private void OnEnable()
    {
        Evaluate();
    }

    public void Evaluate()
    {
        if (SteamIntegration.instance != null)
        {
            if (!SteamIntegration.instance.IsFullVersion || LobbyManager.instance && LobbyManager.instance.isDemoLobby)
            {
                Selectable uiElement = GetComponent<Selectable>();

                if (uiElement != null)
                {
                    uiElement.interactable = false;

                    if (uiElement is Toggle toggleElement && turnOffIfToggle)
                    {
                        toggleElement.isOn = false;
                    }
                    OnInteractionDisabled?.Invoke();
                }
                else
                {
                    Debug.LogWarning($"No Selectable UI component found on {gameObject.name}", gameObject);
                }
            }
        }
    }
}