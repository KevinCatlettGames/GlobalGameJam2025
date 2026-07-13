using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;

public class SwitchUIInputEnforcer : MonoBehaviour
{
    [Header("Control Scheme Names")]
    [SerializeField] private string switchControlSchemeName = "Switch";

    private InputSystemUIInputModule uiInputModule;

    void Awake()
    {
#if UNITY_SWITCH && !UNITY_EDITOR
        uiInputModule = GetComponent<InputSystemUIInputModule>();
        if (uiInputModule == null) return;

        ConfigureUIControlScheme();
#endif
    }

    private void ConfigureUIControlScheme()
    {
        string schemeToEnforce = switchControlSchemeName;

        // Get the actual asset instance used by the UI Input Module
        InputActionAsset asset = uiInputModule.actionsAsset;
        if (asset == null) return;

        // Find the scheme to extract its binding group string
        var targetScheme = asset.FindControlScheme(schemeToEnforce);

        if (targetScheme.HasValue)
        {
            // Disable the module safely while altering the asset
            uiInputModule.enabled = false;

            // Apply the mask directly to the asset. This forces the asset to 
            // ONLY execute bindings belonging to this control scheme's group.
            asset.bindingMask = InputBinding.MaskByGroup(targetScheme.Value.bindingGroup);

            // Re-enable the module to process the changes
            uiInputModule.enabled = true;

            Debug.Log($"[UI Input] Successfully masked InputActionAsset to: {schemeToEnforce}");
        }
        else
        {
            Debug.LogError($"[UI Input] Control Scheme '{schemeToEnforce}' not found in {asset.name}");
        }
    }
}