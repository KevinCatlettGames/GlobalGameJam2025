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
    public Toggle plateToggle;
    public Toggle potToggle;
    public Slider bucketSlider;

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
        mainLobbyUI.SetActive(false);
        SetTab(Tab.General);
    }

    private void OnDisable()
    {
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

        if (!tabTogglingEnabled)
            EnableTabToggling();

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

        bool scoreAtMax =
            Mathf.Approximately(
                scoreToWinSlider.value,
                scoreToWinSlider.maxValue
            );

        Navigation endlessNav = endlessToggle.navigation;
        endlessNav.mode = Navigation.Mode.Explicit;

        endlessNav.selectOnRight =
            isCustom ? leftSpellButton : loadoutButton;

        endlessToggle.navigation = endlessNav;

        Navigation scoreNav = scoreToWinSlider.navigation;
        scoreNav.mode = Navigation.Mode.Explicit;

        scoreNav.selectOnRight =
            isCustom && scoreAtMax
                ? rightSpellButton
                : null;

        scoreNav.selectOnRight =
          !isCustom && scoreAtMax
              ? loadoutButton
              : null;

        scoreToWinSlider.navigation = scoreNav;
    }

    private void SetButtonNavigation(Tab tab)
    {
        Navigation generalNav = generalButton.navigation;
        Navigation spellNav = spellButton.navigation;
        Navigation mapsNav = mapsButton.navigation;
        Navigation backNav = backButton.navigation;

        generalNav.mode = Navigation.Mode.Explicit;
        spellNav.mode = Navigation.Mode.Explicit;
        mapsNav.mode = Navigation.Mode.Explicit;
        backNav.mode = Navigation.Mode.Explicit;

        switch (tab)
        {
            case Tab.General:
                generalNav.selectOnDown = gameModeButton;
                spellNav.selectOnDown = loadoutButton;
                mapsNav.selectOnDown = loadoutButton;
                backNav.selectOnUp = scoreToWinSlider;
                EventSystem.current.SetSelectedGameObject(generalButton.gameObject);
                break;

            case Tab.Spells:
                generalNav.selectOnDown = explosionToggle;
                spellNav.selectOnDown = giantToggle;
                mapsNav.selectOnDown = giantToggle;
                backNav.selectOnUp = grenadeToggle;
                EventSystem.current.SetSelectedGameObject(spellButton.gameObject);
                break;

            case Tab.Maps:
                generalNav.selectOnDown = plateToggle;
                spellNav.selectOnDown = plateToggle;
                mapsNav.selectOnDown = potToggle;
                backNav.selectOnUp = bucketSlider;
                EventSystem.current.SetSelectedGameObject(mapsButton.gameObject);
                break;

            default:
                generalNav.selectOnDown = backButton;
                spellNav.selectOnDown = backButton;
                mapsNav.selectOnDown = backButton;
                backNav.selectOnUp = generalButton;
                EventSystem.current.SetSelectedGameObject(generalButton.gameObject);
                break;
        }

        generalButton.navigation = generalNav;
        spellButton.navigation = spellNav;
        mapsButton.navigation = mapsNav;
        backButton.navigation = backNav;
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

    private void ChangeTab(bool forward)
    {
        if (!tabTogglingEnabled) return;

        if (!SteamIntegration.instance.IsFullVersion)
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
        {
            currentTab = forward
            ? (currentTab == Tab.General ? Tab.Spells :
               currentTab == Tab.Spells ? Tab.Maps : Tab.General)
            : (currentTab == Tab.General ? Tab.Maps :
               currentTab == Tab.Maps ? Tab.Spells : Tab.General);
        }

        SetTab(currentTab);
    }

    public void ToggleRoundsToWinOptionInverted(bool value)
    {
        roundsToWinOption.SetActive(!value);
    }
}