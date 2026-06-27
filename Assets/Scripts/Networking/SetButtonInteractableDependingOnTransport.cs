using UnityEngine;
using UnityEngine.UI; 

/// <summary>
/// Controls the interactable state of a UI button based on the currently active network transport.
/// Listens to transport switch events and enables or disables the button accordingly.
/// </summary>
public class SetButtonInteractableDependingOnTransport : MonoBehaviour
{
    /// <summary>
    /// Reference to the singleton TransportSwitcher that manages network transports.
    /// </summary>
    private TransportSwitcher transportSwitcher;

    /// <summary>
    /// The UI button whose interactable state will be modified.
    /// </summary>
    public Button button;

    /// <summary>
    /// Unity Start method. Initializes the transportSwitcher reference and subscribes
    /// to transport switch events.
    /// </summary>
    private void Start()
    {
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && !UNITY_SWITCH

        transportSwitcher = TransportSwitcher.Instance;
        transportSwitcher.onSwitchToRelayTransport.AddListener(MakeInteractable);
        transportSwitcher.onSwitchToUnityTransport.AddListener(MakeNonInteractable);
#endif
    }

    /// <summary>
    /// Unity OnDisable method. Unsubscribes from transport switch events
    /// to prevent memory leaks.
    /// </summary>
    private void OnDisable()
    {
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && !UNITY_SWITCH

        if (TransportSwitcher.Instance)
        {
            transportSwitcher.onSwitchToRelayTransport.RemoveListener(MakeInteractable);
            transportSwitcher.onSwitchToUnityTransport.RemoveListener(MakeNonInteractable);
        }
#endif
    }

    /// <summary>
    /// Makes the assigned button interactable.
    /// </summary>
    private void MakeInteractable()
    {
        button.interactable = true; 
    }

    /// <summary>
    /// Makes the assigned button non-interactable.
    /// </summary>
    private void MakeNonInteractable()
    {
        button.interactable = false; 
    }
}