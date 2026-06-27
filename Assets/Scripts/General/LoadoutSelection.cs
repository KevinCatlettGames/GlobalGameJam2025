using FMODUnity;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.PropertyVariants.TrackedProperties;
using UnityEngine.UI;

public class LoadoutSelection : MonoBehaviour
{
    private LobbyManager lobbyManager;

    public enum LoadOutType
    {
        SharedRandom,
        IndividualRandom,
        SharedCustom
    }

    [Header("State")]
    [SerializeField]
    private LoadOutType selectedLoadoutType =
        LoadOutType.SharedRandom;

    [Header("UI")]
    [SerializeField] private TextMeshProUGUI loadoutText;
    public LocalizeStringEvent loadoutTextStringEvent;
    [SerializeField] private LocalizedStringProperty sharedRandomLocalizedStringProperty;
    [SerializeField] private LocalizedStringProperty individualRandomLocalizedStringProperty;
    [SerializeField] private LocalizedStringProperty sharedCustomLocalizedStringProperty;

    [SerializeField] private GameObject customLoadoutSelection;

    [SerializeField] private Image leftSpellImage;
    [SerializeField] private Image rightSpellImage;

    [Header("Buttons")]
    [SerializeField] private Button loadoutButton;
    [SerializeField] private Button leftSpellButton;
    [SerializeField] private Button rightSpellButton;

    public Button gameModeButton;
    public Toggle endlessToggle;
    public Slider roundsToWinSlider;

    [Header("Input")]
    [SerializeField] private InputActionProperty loadoutSwitchInputAction;
    [SerializeField] private float stickThreshold = 0.5f;
    [SerializeField] private float pageInitialDelay = 0.35f;
    [SerializeField] private float pageRepeatRate = 0.12f;

    [Header("Bubbles")]
    [SerializeField] private Image[] loadoutBubbleImages;
    [SerializeField] private Image[] leftSpellBubbles;
    [SerializeField] private Image[] rightSpellBubbles;

    [SerializeField] private Sprite activePageDotSprite;
    [SerializeField] private Color activePageDotColor;
    [SerializeField] private Sprite inactivePageDotSprite;
    [SerializeField] private Color inactivePageDotColor;

    private bool stickInUse;
    private float pageHoldTimer;
    public StudioEventEmitter buttonClickEmitter;

    private void Start()
    {
        lobbyManager = LobbyManager.instance;

        ApplyAllUI();
        RefreshNavigation();
    }

    private void OnEnable()
    {
        loadoutSwitchInputAction.action.Enable();
    }

    private void OnDisable()
    {
        loadoutSwitchInputAction.action.Disable();
    }

    //private void Update()
    //{
    //    Vector2 stick =
    //        loadoutSwitchInputAction.action.ReadValue<Vector2>();

    //    int direction = 0;

    //    if (stick.x < -stickThreshold)
    //        direction = -1;
    //    else if (stick.x > stickThreshold)
    //        direction = 1;

    //    if (direction != 0)
    //    {
    //        GameObject currentSelected =
    //            EventSystem.current.currentSelectedGameObject;

    //        if (!stickInUse)
    //        {
    //            pageHoldTimer = 0f;
    //            HandleSelectionInput(currentSelected, direction);
    //            stickInUse = true;
    //        }
    //        else
    //        {
    //            pageHoldTimer += Time.deltaTime;

    //            if (pageHoldTimer >= pageInitialDelay)
    //            {
    //                HandleSelectionInput(currentSelected, direction);
    //                pageHoldTimer = pageInitialDelay - pageRepeatRate;
    //            }
    //        }
    //    }
    //    else
    //    {
    //        stickInUse = false;
    //        pageHoldTimer = 0f;
    //    }
    //}

    private void HandleSelectionInput(GameObject currentSelected, int direction)
    {
        bool increment = direction > 0;

        if (currentSelected == loadoutButton.gameObject)
            ChangeLoadOutType(increment);
        else if (currentSelected == leftSpellButton.gameObject)
            ChangeLeftSpell(increment);
        else if (currentSelected == rightSpellButton.gameObject)
            ChangeRightSpell(increment);

        buttonClickEmitter?.Play();
    }
    public void OnLoadoutButtonClick()
    {
        ChangeLoadOutType(true, true);
        buttonClickEmitter?.Play();
    }

    public void OnLeftSpellButtonClick()
    {
        ChangeLeftSpell(true, true);
        buttonClickEmitter?.Play();
    }

    public void OnRightSpellButtonClick()
    {
        ChangeRightSpell(true, true);
        buttonClickEmitter?.Play();
    }

    public void ChangeLoadOutType(bool increment, bool allowNegativeLoop = false)
    {
        int count = Enum.GetValues(typeof(LoadOutType)).Length;
        int index = (int)selectedLoadoutType;

        if (increment)
        {
            index = (index + 1) % count;
        }
        else
        {
            index = allowNegativeLoop
                ? (index - 1 + count) % count
                : Mathf.Max(index - 1, 0);
        }

        selectedLoadoutType = (LoadOutType)index;
        lobbyManager.selectedLoadoutType = selectedLoadoutType;

        ApplyAllUI();
        RefreshNavigation();
        buttonClickEmitter?.Play();
    }
    public void ChangeLeftSpell(bool increment, bool allowNegativeLoop = false)
    {
        int count = lobbyManager.Spells.Length;
        int index = lobbyManager.selectedLeftSpellIndex;

        if (increment)
            index = (index + 1) % count;
        else
            index = allowNegativeLoop
                ? (index - 1 + count) % count
                : Mathf.Max(index - 1, 0);

        lobbyManager.selectedLeftSpellIndex = index;

        ApplyAllUI();
        RefreshNavigation();
    }

    public void ChangeRightSpell(bool increment, bool allowNegativeLoop = false)
    {
        int count = lobbyManager.Spells.Length;
        int index = lobbyManager.selectedRightSpellIndex;

        if (increment)
            index = (index + 1) % count;
        else
            index = allowNegativeLoop
                ? (index - 1 + count) % count
                : Mathf.Max(index - 1, 0);

        lobbyManager.selectedRightSpellIndex = index;

        ApplyAllUI();
        RefreshNavigation();
    }

    private void ApplyAllUI()
    {

        switch(selectedLoadoutType)
        {
            case LoadOutType.SharedCustom:
                loadoutTextStringEvent.StringReference = sharedCustomLocalizedStringProperty.LocalizedString;               
                    break;
            case LoadOutType.SharedRandom:
                loadoutTextStringEvent.StringReference = sharedRandomLocalizedStringProperty.LocalizedString;
                break;
            case LoadOutType.IndividualRandom:
                loadoutTextStringEvent.StringReference = individualRandomLocalizedStringProperty.LocalizedString;
                break;
        }

        for (int i = 0; i < loadoutBubbleImages.Length; i++)
        {
            bool active = i == (int)selectedLoadoutType;

            loadoutBubbleImages[i].sprite =
                active ? activePageDotSprite : inactivePageDotSprite;

            loadoutBubbleImages[i].color =
                active ? activePageDotColor : inactivePageDotColor;
        }

        bool isCustom = selectedLoadoutType == LoadOutType.SharedCustom;
        customLoadoutSelection.SetActive(isCustom);

        if (isCustom)
        {
            leftSpellImage.sprite =
                lobbyManager.Spells[lobbyManager.selectedLeftSpellIndex].SpellIcon;

            rightSpellImage.sprite =
                lobbyManager.Spells[lobbyManager.selectedRightSpellIndex].SpellIcon;

            UpdateSpellBubbles();
        }
    }

    private void RefreshNavigation()
    {
        SetLoadoutButtonNav();
        SetLeftSpellButtonNav();
        SetRightSpellButtonNav();
        MatchSettingsSelection.Instance.ApplyLoadoutConditionalNavigation();
    }

    private void SetLoadoutButtonNav()
    {
        Navigation nav = loadoutButton.navigation;

        if (selectedLoadoutType == LoadOutType.SharedCustom)
        {
            nav.selectOnDown = leftSpellButton;
        }
        else
        {
            nav.selectOnLeft = gameModeButton;
            nav.selectOnDown = null;
        }
        loadoutButton.navigation = nav;
    }

    private void SetLeftSpellButtonNav()
    {
        Navigation nav = leftSpellButton.navigation;
        nav.mode = Navigation.Mode.Explicit;

        nav.selectOnLeft = endlessToggle;

        leftSpellButton.navigation = nav;
    }

    private void SetRightSpellButtonNav()
    {
        Navigation nav = rightSpellButton.navigation;
        nav.mode = Navigation.Mode.Explicit;

        nav.selectOnLeft = roundsToWinSlider;

        rightSpellButton.navigation = nav;
    }

    private void UpdateSpellBubbles()
    {
        for (int i = 0; i < leftSpellBubbles.Length; i++)
        {
            bool active = i == lobbyManager.selectedLeftSpellIndex;

            leftSpellBubbles[i].sprite =
                active ? activePageDotSprite : inactivePageDotSprite;

            leftSpellBubbles[i].color =
                active ? activePageDotColor : inactivePageDotColor;
        }

        for (int i = 0; i < rightSpellBubbles.Length; i++)
        {
            bool active = i == lobbyManager.selectedRightSpellIndex;

            rightSpellBubbles[i].sprite =
                active ? activePageDotSprite : inactivePageDotSprite;

            rightSpellBubbles[i].color =
                active ? activePageDotColor : inactivePageDotColor;
        }
    }
}