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

    [Header("System References")]
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private GameObject mainMenuButtons;

    [Header("Tab Buttons")]
    [SerializeField] private Button generalTabButton;
    [SerializeField] private Button weaponsTabButton;
    [SerializeField] private Button mapsTabButton;
    [SerializeField] private Button howToPlayButton;

    [Header("Media Display")]
    [SerializeField] private VideoPlayer videoPlayer;
    [SerializeField] private RawImage itemRawImage;
    [SerializeField] private Image itemMainImage;
    [SerializeField] private Image itemSpriteImage;
    [SerializeField] private TextMeshProUGUI itemHeaderText;
    [SerializeField] private TextMeshProUGUI itemDescriptionText;
    [SerializeField] private Button itemButton;
    
    [Header("Item Data Sets")]
    [SerializeField] private HowToPlayItemSO[] generalItems;
    [SerializeField] private HowToPlayItemSO[] weaponItems;
    [SerializeField] private HowToPlayItemSO[] mapItems;

    [Header("Input Actions")]
    [SerializeField] private InputActionProperty leftSwitchAction;
    [SerializeField] private InputActionProperty rightSwitchAction;

    [Header("Page Dots")]
    [SerializeField] private GameObject[] pageDots;
    [SerializeField] private Sprite activePageDotSprite;
    [SerializeField] private Sprite inactivePageDotSprite;

    private int currentIndex;
    private bool indexTogglingEnabled = false;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (videoPlayer.targetTexture != null)
            itemRawImage.texture = videoPlayer.targetTexture;
    }

    private void OnEnable()
    {
        SetTab(Tab.General);
    }

    private void OnDisable()
    {
        DisableIndexToggling();
        mainMenuButtons.SetActive(true);
        eventSystem.SetSelectedGameObject(howToPlayButton.gameObject);
        itemRawImage.enabled = false;
        itemSpriteImage.enabled = false;
        itemHeaderText.enabled = false;
        itemDescriptionText.enabled = false;
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

    private void OnLeftSwitch(InputAction.CallbackContext ctx) => ChangeIndex(false);
    private void OnRightSwitch(InputAction.CallbackContext ctx) => ChangeIndex(true);

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

        HowToPlayItemSO[] items = GetActiveItems();
        if (items == null || items.Length == 0) return;

        foreach (GameObject go in pageDots)
            go.SetActive(false);

        for (int i = 0; i < items.Length; i++)
            pageDots[i].SetActive(true);

        UpdatePageDotSprites();
    }

    private void ChangeIndex(bool forward)
    {
        HowToPlayItemSO[] items = GetActiveItems();
        if (items == null || items.Length == 0) return;

        currentIndex = forward
            ? (currentIndex + 1) % items.Length
            : (currentIndex - 1 + items.Length) % items.Length;

        foreach (GameObject go in pageDots)
            go.SetActive(false);

        for (int i = 0; i < items.Length; i++)
            pageDots[i].SetActive(true);

        UpdatePageDotSprites();
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

        bool hasVideo = item.ItemClip != null;
        itemRawImage.enabled = hasVideo;

        videoPlayer.Stop();
        videoPlayer.clip = item.ItemClip;

        if (hasVideo)
        {
            videoPlayer.Prepare();
            videoPlayer.prepareCompleted += _ => videoPlayer.Play();
        }

        bool hasMainImage = item.ItemMainImage != null;
        itemMainImage.enabled = hasMainImage;
        itemMainImage.sprite = hasMainImage ? item.ItemMainImage : null;

        bool hasSprite = item.ItemSprite != null;
        itemSpriteImage.enabled = hasSprite;
        itemSpriteImage.sprite = hasSprite ? item.ItemSprite : null;

        bool hasTitle = !string.IsNullOrWhiteSpace(item.ItemName);
        itemHeaderText.enabled = hasTitle;
        itemHeaderText.text = hasTitle ? item.ItemName : string.Empty;

        bool hasDescription = !string.IsNullOrWhiteSpace(item.ItemDescription);
        itemDescriptionText.enabled = hasDescription;
        itemDescriptionText.text = hasDescription ? item.ItemDescription : string.Empty;

        bool useButton = item.UseButton;
        if (useButton)
        {
            itemButton.gameObject.SetActive(true);
            itemButton.GetComponentInChildren<TextMeshProUGUI>().text = item.ButtonText;
            if(!string.IsNullOrWhiteSpace(item.LevelToLoad)) 
                item.SetUpButtonWithLevelLoading(itemButton);
        }
        else
        {
            itemButton.onClick.RemoveAllListeners();
            itemButton.gameObject.SetActive(false);
        }
    }

    private void UpdatePageDotSprites()
    {
        HowToPlayItemSO[] items = GetActiveItems();
        if (items == null || items.Length == 0) return;

        for (int i = 0; i < pageDots.Length; i++)
        {
            var img = pageDots[i].GetComponent<Image>();
            if (img != null)
                img.sprite = inactivePageDotSprite;
        }

        if (currentIndex >= 0 && currentIndex < pageDots.Length)
        {
            var img = pageDots[currentIndex].GetComponent<Image>();
            if (img != null)
                img.sprite = activePageDotSprite;
        }
    }

    public void OpenGeneralTab() => SetTab(Tab.General);
    public void OpenWeaponsTab() => SetTab(Tab.Weapons);
    public void OpenMapsTab() => SetTab(Tab.Maps);
}