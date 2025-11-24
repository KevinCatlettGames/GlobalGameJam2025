using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Video;

public class HowToPlayMenu : MonoBehaviour
{
    public static HowToPlayMenu Instance;

    public enum Tab { General, Weapons, Maps }
    public Tab currentTab = Tab.General;

    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private GameObject mainMenuButtons;

    [SerializeField] private Button generalTabButton;
    [SerializeField] private Button weaponsTabButton;
    [SerializeField] private Button mapsTabButton;
    [SerializeField] private Button howToPlayButton;

    [SerializeField] private InputActionProperty leftTabInputAction;
    [SerializeField] private InputActionProperty rightTabInputAction;

    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage videoRawImage;

    [SerializeField] private Image headerImage;
    [SerializeField] private TextMeshProUGUI headerText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    [SerializeField] private HowToPlayItemSO[] generalItems;
    [SerializeField] private HowToPlayItemSO[] weaponItems;
    [SerializeField] private HowToPlayItemSO[] mapItems;

    [SerializeField] private InputActionProperty leftSwitchAction;
    [SerializeField] private InputActionProperty rightSwitchAction;

    private int currentIndex;
    private bool indexTogglingEnabled = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (videoPlayer.targetTexture != null)
            videoRawImage.texture = videoPlayer.targetTexture;
    }

    private void OnEnable()
    {
        SetTab(Tab.General);
        leftTabInputAction.action.Enable();
        leftTabInputAction.action.performed += OnLeftTab;
        rightTabInputAction.action.Enable();
        rightTabInputAction.action.performed += OnRightTab;
    }

    private void OnDisable()
    {
        DisableIndexToggling();
        leftTabInputAction.action.performed -= OnLeftTab;
        rightTabInputAction.action.performed -= OnRightTab;
        leftTabInputAction.action.Disable();
        rightTabInputAction.action.Disable();

        mainMenuButtons.SetActive(true);
        eventSystem.SetSelectedGameObject(howToPlayButton.gameObject);

        videoRawImage.enabled = false;
        headerImage.enabled = false;
        headerText.enabled = false;
        descriptionText.enabled = false;
    }

    private void OnLeftTab(InputAction.CallbackContext ctx)
    {
        if (currentTab == Tab.General) SetTab(Tab.Maps);
        else if (currentTab == Tab.Weapons) SetTab(Tab.General);
        else SetTab(Tab.Weapons);
    }

    private void OnRightTab(InputAction.CallbackContext ctx)
    {
        if (currentTab == Tab.General) SetTab(Tab.Weapons);
        else if (currentTab == Tab.Weapons) SetTab(Tab.Maps);
        else SetTab(Tab.General);
    }

    public void SetTab(Tab tab)
    {
        currentTab = tab;

        eventSystem.SetSelectedGameObject(tab switch
        {
            Tab.General => generalTabButton.gameObject,
            Tab.Weapons => weaponsTabButton.gameObject,
            Tab.Maps => mapsTabButton.gameObject,
            _ => generalTabButton.gameObject
        });
        
        mainMenuButtons.SetActive(false);

        if (!indexTogglingEnabled)
            EnableIndexToggling();

        currentIndex = 0;
        Display(currentIndex);
    }

    private void EnableIndexToggling()
    {
        indexTogglingEnabled = true;
        leftSwitchAction.action.Enable();
        rightSwitchAction.action.Enable();
        leftSwitchAction.action.performed += OnLeftSwitch;
        rightSwitchAction.action.performed += OnRightSwitch;
    }

    private void DisableIndexToggling()
    {
        indexTogglingEnabled = false;
        leftSwitchAction.action.Disable();
        rightSwitchAction.action.Disable();
        leftSwitchAction.action.performed -= OnLeftSwitch;
        rightSwitchAction.action.performed -= OnRightSwitch;
    }

    private void OnLeftSwitch(InputAction.CallbackContext ctx) => ChangeIndex(false);
    private void OnRightSwitch(InputAction.CallbackContext ctx) => ChangeIndex(true);

    private void ChangeIndex(bool forward)
    {
        HowToPlayItemSO[] items = GetActiveItems();
        if (items == null || items.Length == 0) return;

        currentIndex = forward
            ? (currentIndex + 1) % items.Length
            : (currentIndex - 1 + items.Length) % items.Length;

        Display(currentIndex);
    }

    private HowToPlayItemSO[] GetActiveItems()
    {
        return currentTab switch
        {
            Tab.General => generalItems,
            Tab.Weapons => weaponItems,
            Tab.Maps => mapItems,
            _ => null
        };
    }

    private void Display(int index)
    {
        var items = GetActiveItems();
        if (items == null || items.Length == 0) return;

        var item = items[index];

        // VIDEO
        bool hasVideo = item.ItemClip != null;
        videoRawImage.enabled = hasVideo;

        videoPlayer.Stop();
        videoPlayer.clip = item.ItemClip;

        if (hasVideo)
        {
            videoPlayer.Prepare();
            videoPlayer.prepareCompleted += _ => videoPlayer.Play();
        }

        // IMAGE
        bool hasSprite = item.ItemSprite != null;
        headerImage.enabled = hasSprite;
        headerImage.sprite = hasSprite ? item.ItemSprite : null;

        // HEADER
        bool hasTitle = !string.IsNullOrWhiteSpace(item.ItemName);
        headerText.enabled = hasTitle;
        headerText.text = hasTitle ? item.ItemName : string.Empty;

        // DESCRIPTION
        bool hasDescription = !string.IsNullOrWhiteSpace(item.ItemDescription);
        descriptionText.enabled = hasDescription;
        descriptionText.text = hasDescription ? item.ItemDescription : string.Empty;
    }

    public void OpenGeneralTab() => SetTab(Tab.General);
    public void OpenWeaponsTab() => SetTab(Tab.Weapons);
    public void OpenMapsTab() => SetTab(Tab.Maps);
}