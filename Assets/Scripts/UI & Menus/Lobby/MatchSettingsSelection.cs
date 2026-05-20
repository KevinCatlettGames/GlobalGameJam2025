using UnityEngine;
using FMODUnity;
using Unity.Netcode;
using UnityEngine.InputSystem; 
using UnityEngine.UI; 

public class MatchSettingsSelection : NetworkBehaviour
{
    public static MatchSettingsSelection Instance;
    public enum Tab {General, Weapons, Maps}
    public Tab currentTab = Tab.General;
    
    [SerializeField] private Color activeColor;
    [SerializeField] private Color inactiveColor;
    [SerializeField] private GameObject generalTabFrame;
    [SerializeField] private GameObject generalTab;

    [SerializeField] private GameObject weaponsTabFrame;
    [SerializeField] private GameObject weaponsTab;

    [SerializeField] private GameObject mapsTabFrame;
    [SerializeField] private GameObject mapsTab;

    [SerializeField] private GameObject lbFrame;
    [SerializeField] private GameObject rbFrame;
    
    [SerializeField] private InputActionProperty leftTabSwitchAction;
    [SerializeField] private InputActionProperty rightTabSwitchAction;
    private bool tabTogglingEnabled = false;
    
    [Header("UI")]
    [SerializeField] StudioEventEmitter buttonOnClickEmitter;
    
    [Header("Input")]
    public InputActionProperty exitMatchSettingsInputAction;
    
    [Header("Lobby Connection")] 
    [SerializeField] LobbyButtons lobbyButtons;

    public GameObject mainLobbyUI; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        mainLobbyUI.SetActive(false);
        SetTab(Tab.General);
    }

    private void OnDisable()
    {
        mainLobbyUI.SetActive(true);
        DisableTabToggling();
    }
    
    #region Tab Switching
    private void OnLeftTabSwitch(InputAction.CallbackContext ctx)
    {
        if (ctx.canceled) return;
        ChangeTab(false);
    }

    private void OnRightTabSwitch(InputAction.CallbackContext ctx)
    {
        if (ctx.canceled) return;
        ChangeTab(true);
    }

    private void EnableTabToggling()
    {
        tabTogglingEnabled = true;

        leftTabSwitchAction.action.Enable();
        rightTabSwitchAction.action.Enable();
        
        leftTabSwitchAction.action.performed += OnLeftTabSwitch;
        rightTabSwitchAction.action.performed += OnRightTabSwitch;
    }

    private void DisableTabToggling()
    {
        tabTogglingEnabled = false;

        leftTabSwitchAction.action.Disable();
        rightTabSwitchAction.action.Disable();
        
        leftTabSwitchAction.action.performed -= OnLeftTabSwitch;
        rightTabSwitchAction.action.performed -= OnRightTabSwitch;

    }

    private void ChangeTab(bool forward)
    {
        if (!tabTogglingEnabled) return;

        Tab nextTab = forward
            ? (currentTab == Tab.General ? Tab.Weapons :
                currentTab == Tab.Weapons ? Tab.Maps : Tab.General)
            : (currentTab == Tab.General ? Tab.Maps :
                currentTab == Tab.Maps ? Tab.Weapons : Tab.General);

        SetTab(nextTab);
    }
    #endregion
    
    public void SetTab(Tab tab)
    {
        currentTab = tab;
        
        if (!tabTogglingEnabled)
            EnableTabToggling();
        
        generalTabFrame.GetComponent<Image>().color = (currentTab == Tab.General) ? inactiveColor : activeColor;
        weaponsTabFrame.GetComponent<Image>().color = (currentTab == Tab.Weapons) ? inactiveColor : activeColor;
        mapsTabFrame.GetComponent<Image>().color = (currentTab == Tab.Maps) ? inactiveColor : activeColor;
        generalTabFrame.GetComponent<Outline>().enabled = currentTab == Tab.General;
        weaponsTabFrame.GetComponent<Outline>().enabled = currentTab == Tab.Weapons;
        mapsTabFrame.GetComponent<Outline>().enabled = currentTab == Tab.Maps;
        
        switch(tab)
        {
            case Tab.General:
                generalTabFrame.transform.SetAsLastSibling();
                generalTab.SetActive(true);
                weaponsTab.SetActive(false);
                mapsTab.SetActive(false);
                break;
            case Tab.Weapons:
                weaponsTabFrame.transform.SetAsLastSibling();
                generalTab.SetActive(false);
                weaponsTab.SetActive(true);
                mapsTab.SetActive(false);
                break;
            case  Tab.Maps:
                mapsTabFrame.transform.SetAsLastSibling();
                generalTab.SetActive(false);
                weaponsTab.SetActive(false);
                mapsTab.SetActive(true);
                break;
        }
    }

    public void OpenGeneralTab() => SetTab(Tab.General);
    public void OpenWeaponsTab() => SetTab(Tab.Weapons);
    public void OpenMapsTab() => SetTab(Tab.Maps);
}