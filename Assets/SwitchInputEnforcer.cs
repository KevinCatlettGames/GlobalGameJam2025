using UnityEngine;
using UnityEngine.InputSystem;

public class SwitchInputEnforcer : MonoBehaviour
{

    [SerializeField] private PlayerInput playerInput;

    // Make sure these match the EXACT names you typed in your Input Actions editor
    [SerializeField] private string switchControlSchemeName = "Switch";

    void Start()
    {
        if (playerInput == null)
        {
            playerInput = GetComponent<PlayerInput>();
        }

        ConfigureControlScheme();
    }

    private void ConfigureControlScheme()
    {
        if (playerInput == null) return;

#if UNITY_SWITCH && !UNITY_EDITOR
        // We are running natively on a Nintendo Switch console
        playerInput.SwitchCurrentControlScheme(switchControlSchemeName);
        
        // Optional: If you want to lock it so it doesn't accidentally 
        // switch back to keyboard/mouse if it detects a random input hook
        playerInput.neverAutoSwitchControlSchemes = true; 
        
        Debug.Log($"[Input] Native Switch detected. Enforced control scheme: {switchControlSchemeName}");
#endif
    }
}