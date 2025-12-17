using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.Video;
using FMODUnity;

public class HowToPlayMenu : MonoBehaviour
{
    public static HowToPlayMenu Instance;

    public enum Tab { General, Weapons, Maps }
    public Tab currentTab = Tab.General;

    [Header("System References")]
    [SerializeField] private GameObject mainMenuButtons;
    [SerializeField] private Image mainMenuBackground;
    [SerializeField] private Color mainMenuBackgroundColor;
    [SerializeField] private Color mainMenuBackgroundDarkColor;
    
    [Header("Tab Buttons")]
    [SerializeField] private Button howToPlayButton;
    [SerializeField] private Color activeColor;
    [SerializeField] private Color inactiveColor;
    [SerializeField] private GameObject generalTabFrame;
    [SerializeField] private GameObject weaponsTabFrame;
    [SerializeField] private GameObject mapsTabFrame;
    [SerializeField] private GameObject lbFrame;
    [SerializeField] private GameObject rbFrame;
    [SerializeField] private Image mainElementBackground;
    
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
    [SerializeField] private InputActionProperty leftTabSwitchAction;
    [SerializeField] private InputActionProperty rightTabSwitchAction;
    [SerializeField] private InputActionProperty leftPageSwitchAction;

    [Header("Page Dots")]
    [SerializeField] private GameObject[] pageDots;
    [SerializeField] private Sprite activePageDotSprite;
    [SerializeField] private Color activePageDotColor; 
    [SerializeField] private Sprite inactivePageDotSprite;
    [SerializeField] private Color inactivePageDotColor;
    
    private int currentIndex;

    private bool pageTogglingEnabled = false;
    private bool tabTogglingEnabled = false;

    [Header("Smooth Page Switch")]
    [SerializeField] private float stickThreshold = 0.5f;      // Minimum stick tilt
    [SerializeField] private float pageInitialDelay = 0.35f;   // Delay before repeating
    [SerializeField] private float pageRepeatRate = 0.12f;     // Repeat speed when held

    [Header("FMOD events")]
    [SerializeField] StudioEventEmitter tabSwitchEmitter;
    [SerializeField] private StudioEventEmitter pageSwitchEmitter;
    
    private bool stickInUse = false;
    private float pageHoldTimer = 0f;
    private int pageDirection = 0;
    
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
        mainMenuBackground.color = mainMenuBackgroundDarkColor;
    }

    private void OnDisable()
    {
        DisablePageToggling();
        DisableTabToggling();

        mainMenuButtons.SetActive(true);
        itemRawImage.enabled = false;
        itemSpriteImage.enabled = false;
        itemHeaderText.enabled = false;
        itemDescriptionText.enabled = false;
        mainMenuBackground.color = mainMenuBackgroundColor;
    }

    private void OnCloseHowToPlay(InputAction.CallbackContext ctx)
    {
        gameObject.SetActive(false);
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

    #region Page Switching
    private void EnablePageToggling()
    {
        pageTogglingEnabled = true;
        leftPageSwitchAction.action.Enable();
    }

    private void DisablePageToggling()
    {
        pageTogglingEnabled = false;
        leftPageSwitchAction.action.Disable();
    }

    private void Update()
    {
        if (!pageTogglingEnabled) return;

        Vector2 stick = leftPageSwitchAction.action.ReadValue<Vector2>();
        int direction = 0;

        if (stick.x < -stickThreshold) direction = -1;
        else if (stick.x > stickThreshold) direction = 1;

        if (direction != 0)
        {
            if (!stickInUse)
            {
                // First trigger
                ChangePage(direction > 0);
                pageHoldTimer = 0f;
                stickInUse = true;
            }
            else
            {
                // Holding stick: repeat after initial delay
                pageHoldTimer += Time.deltaTime;
                if (pageHoldTimer >= pageInitialDelay)
                {
                    ChangePage(direction > 0);
                    pageHoldTimer = pageInitialDelay - pageRepeatRate; // maintain repeat interval
                }
            }
        }
        else
        {
            // Stick released
            stickInUse = false;
            pageHoldTimer = 0f;
        }
    }

    private void ChangePage(bool forward)
    {
        HowToPlayItemSO[] items = GetActiveItems();
        if (items == null || items.Length == 0) return;

        currentIndex = forward
            ? (currentIndex + 1) % items.Length
            : (currentIndex - 1 + items.Length) % items.Length;

        UpdatePageDots();
        Display(currentIndex);
        pageSwitchEmitter.Play();
    }
    #endregion

    #region Display / Dots
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
        }
        else
        {
            itemButton.onClick.RemoveAllListeners();
            itemButton.gameObject.SetActive(false);
        }

        mainElementBackground.enabled = item.IncorporateMainElementBackground;
    }

    private void UpdatePageDots()
    {
        HowToPlayItemSO[] items = GetActiveItems();
        if (items == null || items.Length == 0) return;

        for (int i = 0; i < pageDots.Length; i++)
        {
            var img = pageDots[i].GetComponent<Image>();
            if (img != null)
            {
                img.sprite = inactivePageDotSprite;
                img.color = inactivePageDotColor;
            }
        }

        if (currentIndex >= 0 && currentIndex < pageDots.Length)
        {
            var img = pageDots[currentIndex].GetComponent<Image>();
            if (img != null)
            {
                img.sprite = activePageDotSprite;
                img.color = activePageDotColor; 
            }
        }
    }
    #endregion

    public void SetTab(Tab tab)
    {
        currentTab = tab;
        mainMenuButtons.SetActive(false);

        if (!pageTogglingEnabled)
            EnablePageToggling();

        if (!tabTogglingEnabled)
            EnableTabToggling();

        currentIndex = 0;
        Display(currentIndex);

        HowToPlayItemSO[] items = GetActiveItems();
        if (items == null || items.Length == 0) return;

        foreach (GameObject go in pageDots)
            go.SetActive(false);

        for (int i = 0; i < items.Length; i++)
            pageDots[i].SetActive(true);

        UpdatePageDots();
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
                break;
            case Tab.Weapons:
                weaponsTabFrame.transform.SetAsLastSibling();
                break;
            case  Tab.Maps:
                mapsTabFrame.transform.SetAsLastSibling();
                break;
        }

        tabSwitchEmitter.Play();
    }

    public void OpenGeneralTab() => SetTab(Tab.General);
    public void OpenWeaponsTab() => SetTab(Tab.Weapons);
    public void OpenMapsTab() => SetTab(Tab.Maps);
}
