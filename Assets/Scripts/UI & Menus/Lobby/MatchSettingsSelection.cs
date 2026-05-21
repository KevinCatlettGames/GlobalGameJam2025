using FMODUnity;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem; 
using UnityEngine.UI; 

public class MatchSettingsSelection : NetworkBehaviour
{
    public static MatchSettingsSelection Instance;
    public enum Tab {General, Spells, Maps}
    public Tab currentTab = Tab.General;
    
    [SerializeField] private Color activeColor;
    [SerializeField] private Color inactiveColor;
    [SerializeField] private GameObject generalTabFrame;
    [SerializeField] private GameObject generalTab;

    [SerializeField] private GameObject spellsTabFrame;
    [SerializeField] private GameObject spellsTab;

    [SerializeField] private GameObject mapsTabFrame;
    [SerializeField] private GameObject mapsTab;
    
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


    public Button generalButton;
    public Button spellButton;
    public Button mapsButton;
    public Button backButton;
    public Button gameModeButton;
    public Toggle endlessToggle;
    public Toggle explosionToggle;
    public Toggle giantToggle;
    public Toggle grenadeToggle;
    public Toggle plateToggle;
    public Slider bucketSlider; 

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        mainLobbyUI.SetActive(false);
        SetTab(Tab.General);
        SetButtonNavigation(Tab.General);
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
            ? (currentTab == Tab.General ? Tab.Spells :
                currentTab == Tab.Spells ? Tab.Maps : Tab.General)
            : (currentTab == Tab.General ? Tab.Maps :
                currentTab == Tab.Maps ? Tab.Spells : Tab.General);

        SetTab(nextTab);
        SetButtonNavigation(nextTab);
    }
    #endregion
    
    public void SetTab(Tab tab)
    {
        currentTab = tab;
        
        if (!tabTogglingEnabled)
            EnableTabToggling();
        
        switch(tab)
        {
            case Tab.General:
                generalTabFrame.SetActive(true);
                spellsTabFrame.SetActive(false);
                mapsTabFrame.SetActive(false);
                generalTab.SetActive(true);
                spellsTab.SetActive(false);
                mapsTab.SetActive(false);
                break;
            case Tab.Spells:
                generalTabFrame.SetActive(false);
                spellsTabFrame.SetActive(true);
                mapsTabFrame.SetActive(false);
                generalTab.SetActive(false);
                spellsTab.SetActive(true);
                mapsTab.SetActive(false);
                break;
            case  Tab.Maps:
                generalTabFrame.SetActive(false);
                spellsTabFrame.SetActive(false);
                mapsTabFrame.SetActive(true);
                generalTab.SetActive(false);
                spellsTab.SetActive(false);
                mapsTab.SetActive(true);
                break;
        }
    }

    public void OpenGeneralTab() => SetTab(Tab.General);
    public void OpenWeaponsTab() => SetTab(Tab.Spells);
    public void OpenMapsTab() => SetTab(Tab.Maps);

    private void SetButtonNavigation(Tab tab)
    {
        Navigation newGeneralNav = new Navigation();
        newGeneralNav.mode = Navigation.Mode.Explicit;
        newGeneralNav.selectOnDown = generalButton.navigation.selectOnDown;
        newGeneralNav.selectOnLeft = generalButton.navigation.selectOnLeft;
        newGeneralNav.selectOnRight = generalButton.navigation.selectOnRight;

        Navigation newSpellsNav = new Navigation();
        newSpellsNav.mode = Navigation.Mode.Explicit;
        newSpellsNav.selectOnDown = spellButton.navigation.selectOnDown;
        newSpellsNav.selectOnLeft = spellButton.navigation.selectOnLeft;
        newSpellsNav.selectOnRight = spellButton.navigation.selectOnRight;


        Navigation newMapsNav = new Navigation();
        newMapsNav.mode = Navigation.Mode.Explicit;
        newMapsNav.selectOnUp = mapsButton.navigation.selectOnUp;
        newMapsNav.selectOnLeft = mapsButton.navigation.selectOnLeft;
        newMapsNav.selectOnRight = mapsButton.navigation.selectOnRight;

        Navigation newBackNav = new Navigation();
        newBackNav.mode = Navigation.Mode.Explicit;
        newBackNav.selectOnUp = backButton.navigation.selectOnUp;
        newBackNav.selectOnLeft = backButton.navigation.selectOnLeft;
        newBackNav.selectOnRight = backButton.navigation.selectOnRight;

        switch (tab)
        {
            case Tab.General:
                newGeneralNav.selectOnDown = gameModeButton;
                newSpellsNav.selectOnDown = gameModeButton;
                newMapsNav.selectOnDown = gameModeButton;
                newBackNav.selectOnUp = endlessToggle;
                break;
            case Tab.Spells:
                newGeneralNav.selectOnDown = explosionToggle;
                newSpellsNav.selectOnDown = giantToggle;
                newMapsNav.selectOnDown = giantToggle;
                newBackNav.selectOnUp = grenadeToggle;
                break;
            case Tab.Maps:
                newGeneralNav.selectOnDown = plateToggle;
                newSpellsNav.selectOnDown = plateToggle;
                newMapsNav.selectOnDown = plateToggle;
                newBackNav.selectOnUp = bucketSlider;
                break;
            default:
                newGeneralNav.selectOnDown = backButton;
                newSpellsNav.selectOnDown = backButton;
                newMapsNav.selectOnDown = backButton;
                newBackNav.selectOnUp = generalButton; 
                break;
        }
        generalButton.navigation = newGeneralNav;
        spellButton.navigation = newSpellsNav;
        mapsButton.navigation = newMapsNav;
        backButton.navigation = newBackNav;
    }
}