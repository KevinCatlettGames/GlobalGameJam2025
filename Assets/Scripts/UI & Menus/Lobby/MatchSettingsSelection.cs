using FMODUnity;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class MatchSettingsSelection : NetworkBehaviour
{
    public static MatchSettingsSelection Instance;

    public enum Tab { General, Spells, Maps }
    public Tab currentTab = Tab.General;

    [SerializeField] private GameObject mainLobbyUI;

    [Header("Tabs")]
    [SerializeField] private GameObject generalTabFrame;
    [SerializeField] private GameObject generalTab;

    [SerializeField] private GameObject spellsTabFrame;
    [SerializeField] private GameObject spellsTab;

    [SerializeField] private GameObject mapsTabFrame;
    [SerializeField] private GameObject mapsTab;

    [Header("Buttons")]
    public Button generalButton;
    public Button spellButton;
    public Button mapsButton;
    public Button backButton;
    public Button resetButton;
    public Button saveButton;
    public Button gameModeButton;
    public Button loadoutButton;
    public Button leftSpellButton;
    public Button rightSpellButton;

    [Header("Sliders / Toggles")]
    public Slider scoreToWinSlider;
    public Toggle endlessToggle;
    public Toggle explosionToggle;
    public Toggle giantToggle;
    public Toggle grenadeToggle;
    public Toggle blastToggle;
    public Toggle slasherToggle;
    public Toggle plateToggle;
    public Toggle potToggle;
    public Slider bucketSlider;
    public Slider tunaSlider;

    public GameObject roundsToWinOption;
    public StudioEventEmitter tabSwitchEmitter;

    [Header("Input")]
    [SerializeField] private InputActionProperty leftTabSwitchAction;
    [SerializeField] private InputActionProperty rightTabSwitchAction;

    private bool tabTogglingEnabled;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void OnEnable()
    {
        if (mainLobbyUI != null)
            mainLobbyUI.SetActive(false);

        EnableTabToggling();
        SetTab(Tab.General);
    }

    private void OnDisable()
    {
        if (mainLobbyUI != null)
            mainLobbyUI.SetActive(true);

        DisableTabToggling();
    }

    public void SetTab(Tab tab)
    {
        if (!SteamIntegration.instance.IsFullVersion && tab == Tab.Maps)
        {
            switch (currentTab)
            {
                case Tab.General:
                    currentTab = Tab.Spells;
                    break;
                case Tab.Spells:
                    currentTab = Tab.General;
                    break;
            }
        }
        else
            currentTab = tab;

        generalTabFrame.SetActive(tab == Tab.General);
        spellsTabFrame.SetActive(tab == Tab.Spells);
        mapsTabFrame.SetActive(tab == Tab.Maps);

        generalTab.SetActive(tab == Tab.General);
        spellsTab.SetActive(tab == Tab.Spells);
        mapsTab.SetActive(tab == Tab.Maps);
        SetButtonNavigation(tab);
        ApplyLoadoutConditionalNavigation();
        tabSwitchEmitter.Play();
    }

    public void OpenGeneralTab() => SetTab(Tab.General);
    public void OpenWeaponsTab() => SetTab(Tab.Spells);
    public void OpenMapsTab() => SetTab(Tab.Maps);

    public void ApplyLoadoutConditionalNavigation()
    {
        bool isCustom =
            LobbyManager.instance.selectedLoadoutType ==
            LoadoutSelection.LoadOutType.SharedCustom;

        Navigation endlessNav = endlessToggle.navigation;
        endlessNav.mode = Navigation.Mode.Explicit;

        endlessNav.selectOnRight =
            scoreToWinSlider; ;

        endlessToggle.navigation = endlessNav;

        Navigation scoreNav = scoreToWinSlider.navigation;
        scoreNav.mode = Navigation.Mode.Explicit;
    }

    private void SetButtonNavigation(Tab tab)
    {
        Navigation generalNav = generalButton.navigation;
        Navigation spellNav = spellButton.navigation;
        Navigation mapsNav = mapsButton.navigation;
        Navigation resetNav = resetButton.navigation;
        Navigation saveNav = saveButton.navigation;

        generalNav.mode = Navigation.Mode.Explicit;
        spellNav.mode = Navigation.Mode.Explicit;
        mapsNav.mode = Navigation.Mode.Explicit;
        resetNav.mode = Navigation.Mode.Explicit;
        saveNav.mode = Navigation.Mode.Explicit;
 
        switch (tab)
        {
            case Tab.General:
                generalNav.selectOnDown = gameModeButton;
                spellNav.selectOnDown = loadoutButton;
                mapsNav.selectOnDown = loadoutButton;
                resetNav.selectOnUp = endlessToggle;
                saveNav.selectOnUp = scoreToWinSlider;
                EventSystem.current.SetSelectedGameObject(generalButton.gameObject);
                break;

            case Tab.Spells:
                generalNav.selectOnDown = explosionToggle;
                spellNav.selectOnDown = explosionToggle;
                mapsNav.selectOnDown = giantToggle;
                resetNav.selectOnUp = blastToggle;
                saveNav.selectOnUp = slasherToggle;
                EventSystem.current.SetSelectedGameObject(spellButton.gameObject);
                break;

            case Tab.Maps:
                generalNav.selectOnDown = plateToggle;
                spellNav.selectOnDown = plateToggle;
                mapsNav.selectOnDown = potToggle;
                resetNav.selectOnUp = bucketSlider;
                saveNav.selectOnUp = tunaSlider;
                EventSystem.current.SetSelectedGameObject(mapsButton.gameObject);
                break;

            default:
                generalNav.selectOnDown = backButton;
                spellNav.selectOnDown = backButton;
                mapsNav.selectOnDown = backButton;
                EventSystem.current.SetSelectedGameObject(generalButton.gameObject);
                break;
        }

        generalButton.navigation = generalNav;
        spellButton.navigation = spellNav;
        mapsButton.navigation = mapsNav;
        resetButton.navigation = resetNav;
        saveButton.navigation = saveNav;
    }

    private void EnableTabToggling()
    {
        tabTogglingEnabled = true;

        leftTabSwitchAction.action.Enable();
        rightTabSwitchAction.action.Enable();

        leftTabSwitchAction.action.performed -= OnLeftTabSwitch;
        rightTabSwitchAction.action.performed -= OnRightTabSwitch;

        leftTabSwitchAction.action.performed += OnLeftTabSwitch;
        rightTabSwitchAction.action.performed += OnRightTabSwitch;
    }

    private void DisableTabToggling()
    {
        tabTogglingEnabled = false;

        leftTabSwitchAction.action.performed -= OnLeftTabSwitch;
        rightTabSwitchAction.action.performed -= OnRightTabSwitch;
    }

    private void OnLeftTabSwitch(InputAction.CallbackContext ctx)
    {
        if (!tabTogglingEnabled || !ctx.performed) return;
        ChangeTab(false);
    }

    private void OnRightTabSwitch(InputAction.CallbackContext ctx)
    {
        if (!tabTogglingEnabled || !ctx.performed) return;
        ChangeTab(true);
    }

    private void ChangeTab(bool forward)
    {
        if (!tabTogglingEnabled) return;

        currentTab = forward
        ? (currentTab == Tab.General ? Tab.Spells :
           currentTab == Tab.Spells ? Tab.Maps : Tab.General)
        : (currentTab == Tab.General ? Tab.Maps :
           currentTab == Tab.Maps ? Tab.Spells : Tab.General);

        SetTab(currentTab);
    }

    public void ToggleRoundsToWinOptionInverted(bool value)
    {
        roundsToWinOption.SetActive(!value);
    }
}