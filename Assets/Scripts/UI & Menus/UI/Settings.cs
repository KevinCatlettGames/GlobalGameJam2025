using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using FMODUnity;

public class Settings : MonoBehaviour
{
    #region Defaults
    private const float DEFAULT_MASTER = 0.5f;
    private const float DEFAULT_SFX = 0.5f;
    private const float DEFAULT_MUSIC = 0.5f;

    private const bool DEFAULT_FULLSCREEN = true;
    private const int DEFAULT_RESOLUTION = 2;
    private const int DEFAULT_QUALITY = 2;
    #endregion

    #region Pending Settings
    private float pendingMaster;
    private float pendingSfx;
    private float pendingMusic;

    private bool pendingFullscreen;
    private int pendingResolution;
    private int pendingQuality;
    #endregion

    #region Video
    [Header("Video")]
    [SerializeField] private Toggle fullScreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown graphicsQualityDropdown;
    [SerializeField] private int[] resolutionsWidth;
    [SerializeField] private int[] resolutionsHeight;
    #endregion

    #region Audio
    private FMOD.Studio.VCA masterVCA;
    private FMOD.Studio.VCA sfxVCA;
    private FMOD.Studio.VCA musicVCA;

    [Header("Audio")]
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider musicSlider;

    [SerializeField] private TextMeshProUGUI masterValueText;
    [SerializeField] private TextMeshProUGUI sfxValueText;
    [SerializeField] private TextMeshProUGUI musicValueText;
    #endregion

    #region Input
    [Header("Input")]
    [SerializeField] private InputActionProperty leftTabSwitchAction;
    [SerializeField] private InputActionProperty rightTabSwitchAction;
    [SerializeField] private InputActionProperty exitSettingsAction;
    #endregion

    #region Tabs
    [Header("Tabs")]
    [SerializeField] private GameObject videoTab;
    [SerializeField] private GameObject audioTab;
    [SerializeField] private GameObject gameTab;
    [SerializeField] private bool useGameTab = true;
    #endregion

    #region Tab UI
    [Header("Tab UI")]
    [SerializeField] private GameObject videoTabFrame;
    [SerializeField] private GameObject audioTabFrame;
    [SerializeField] private GameObject gameTabFrame;
    #endregion

    #region UI
    [Header("UI")]
    [SerializeField] private GameObject selectedObject;
    [SerializeField] private Button videoButton;
    [SerializeField] private Button audioButton;
    [SerializeField] private Button applyButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Button backButton;
    [SerializeField] private StudioEventEmitter emitter;
    #endregion

    public enum Tab { Video, Audio, Game }

    public Tab currentTab = Tab.Video;
    private bool tabTogglingEnabled;

    private void OnEnable()
    {
        if (exitSettingsAction.action != null)
        {
            exitSettingsAction.action.performed += ExitSettings;
            exitSettingsAction.action.Enable();
        }

        LoadSavedIntoPending();
        ApplyPendingToUI();

        ApplyVideoRuntime();
        ApplyAudioRuntime();

        SetTab(Tab.Video, true);

        UpdateApplyButton();
    }

    private void OnDisable()
    {
        if (exitSettingsAction.action != null)
        {
            exitSettingsAction.action.performed -= ExitSettings;
            exitSettingsAction.action.Disable();
        }

        DisableTabToggling();
    }

    private void Start()
    {
        InitialiseAudio();
        InitialiseVideo();
    }

    private void InitialiseAudio()
    {
        masterVCA = FMODUnity.RuntimeManager.GetVCA("vca:/Master");
        sfxVCA = FMODUnity.RuntimeManager.GetVCA("vca:/SFX");
        musicVCA = FMODUnity.RuntimeManager.GetVCA("vca:/Music");
    }

    private void InitialiseVideo()
    {
        InitialiseResolutions();

        pendingFullscreen = Screen.fullScreen;
        pendingResolution = PlayerPrefs.GetInt("ResolutionLevel", DEFAULT_RESOLUTION);
        pendingQuality = QualitySettings.GetQualityLevel();
    }

    public void SetFullscreen(bool isFullScreen)
    {
        pendingFullscreen = isFullScreen;

        Screen.fullScreen = isFullScreen;

        UpdateApplyButton();
    }

    public void SetResolution(int option)
    {
        pendingResolution = Mathf.Clamp(option, 0, resolutionsWidth.Length - 1);

        Screen.SetResolution(
            resolutionsWidth[pendingResolution],
            resolutionsHeight[pendingResolution],
            pendingFullscreen
        );

        UpdateApplyButton();
    }

    public void SetGraphicsQuality(int option)
    {
        pendingQuality = option;

        QualitySettings.SetQualityLevel(option);
        Application.targetFrameRate = option == 0 ? 60 : -1;

        UpdateApplyButton();
    }

    private void InitialiseResolutions()
    {
        if (resolutionsWidth.Length != resolutionsHeight.Length) return;

        resolutionDropdown.ClearOptions();

        List<string> labels = new();

        for (int i = 0; i < resolutionsWidth.Length; i++)
            labels.Add($"{resolutionsWidth[i]} x {resolutionsHeight[i]}");

        resolutionDropdown.AddOptions(labels);

        int value = PlayerPrefs.GetInt("ResolutionLevel", DEFAULT_RESOLUTION);
        value = Mathf.Clamp(value, 0, resolutionsWidth.Length - 1);

        resolutionDropdown.value = value;
        resolutionDropdown.RefreshShownValue();
    }

    public void SetMasterVolume(float volume)
    {
        pendingMaster = volume * 0.01f;
        masterValueText.text = Mathf.RoundToInt(volume).ToString();

        masterVCA.setVolume(pendingMaster);

        UpdateApplyButton();
    }

    public void SetSFXVolume(float volume)
    {
        pendingSfx = volume * 0.01f;
        sfxValueText.text = Mathf.RoundToInt(volume).ToString();

        sfxVCA.setVolume(pendingSfx);

        UpdateApplyButton();
    }

    public void SetMusicVolume(float volume)
    {
        pendingMusic = volume * 0.01f;
        musicValueText.text = Mathf.RoundToInt(volume).ToString();

        musicVCA.setVolume(pendingMusic);

        UpdateApplyButton();
    }

    private void OnLeftTabSwitch(InputAction.CallbackContext ctx)
    {
        if (!ctx.canceled) ChangeTab(false);
    }

    private void OnRightTabSwitch(InputAction.CallbackContext ctx)
    {
        if (!ctx.canceled) ChangeTab(true);
    }

    private void EnableTabToggling()
    {
        tabTogglingEnabled = true;

        if (leftTabSwitchAction.action != null)
        {
            leftTabSwitchAction.action.Enable();
            leftTabSwitchAction.action.performed += OnLeftTabSwitch;
        }

        if (rightTabSwitchAction.action != null)
        {
            rightTabSwitchAction.action.Enable();
            rightTabSwitchAction.action.performed += OnRightTabSwitch;
        }
    }

    private void DisableTabToggling()
    {
        tabTogglingEnabled = false;

        if (leftTabSwitchAction.action != null)
        {
            leftTabSwitchAction.action.performed -= OnLeftTabSwitch;
            leftTabSwitchAction.action.Disable();
        }

        if (rightTabSwitchAction.action != null)
        {
            rightTabSwitchAction.action.performed -= OnRightTabSwitch;
            rightTabSwitchAction.action.Disable();
        }
    }

    private void ExitSettings(InputAction.CallbackContext obj)
    {
#if !UNITY_SWITCH
        if (resolutionDropdown.IsExpanded || graphicsQualityDropdown.IsExpanded) return; 
#endif 
        backButton.onClick?.Invoke();
    }

    public void SetTab(Tab tab, bool initialSet)
    {
        if (!useGameTab && tab == Tab.Game)
            tab = Tab.Video;

#if UNITY_SWITCH
        tab = Tab.Audio; 
#endif 

        currentTab = tab;

        if (!tabTogglingEnabled)
            EnableTabToggling();

        UpdateTabVisibility(tab);
        SetButtonNavigation(tab);

        if (!initialSet)
            emitter?.Play();
    }

    private void ChangeTab(bool forward)
    {
        if (!tabTogglingEnabled) return;

        Tab nextTab;

        if (useGameTab)
        {
            nextTab = forward
                ? (currentTab == Tab.Video ? Tab.Audio :
                   currentTab == Tab.Audio ? Tab.Game : Tab.Video)
                : (currentTab == Tab.Video ? Tab.Game :
                   currentTab == Tab.Game ? Tab.Audio : Tab.Video);
        }
        else
        {
            nextTab = (currentTab == Tab.Video) ? Tab.Audio : Tab.Video;
        }

        SetTab(nextTab, false);
    }

    private void UpdateTabVisibility(Tab tab)
    {
        videoTab.SetActive(tab == Tab.Video);
        videoTabFrame.SetActive(tab == Tab.Video);

        audioTab.SetActive(tab == Tab.Audio);
        audioTabFrame.SetActive(tab == Tab.Audio);

        gameTab.SetActive(tab == Tab.Game);
        gameTabFrame.SetActive(tab == Tab.Game);
    }

    private void SetButtonNavigation(Tab tab)
    {
        Navigation newApplyNav = new Navigation();
        newApplyNav.mode = Navigation.Mode.Explicit;
        newApplyNav.selectOnDown = applyButton.navigation.selectOnDown;
        newApplyNav.selectOnLeft = applyButton.navigation.selectOnLeft;
        newApplyNav.selectOnRight = applyButton.navigation.selectOnRight;

        Navigation newResetNav = new Navigation();
        newResetNav.mode = Navigation.Mode.Explicit;
        newResetNav.selectOnDown = resetButton.navigation.selectOnDown;
        newResetNav.selectOnLeft = resetButton.navigation.selectOnLeft;
        newResetNav.selectOnRight = resetButton.navigation.selectOnRight;


        Navigation newVideoNav = new Navigation();
        newVideoNav.mode = Navigation.Mode.Explicit;
        newVideoNav.selectOnUp = videoButton.navigation.selectOnUp;
        newVideoNav.selectOnLeft = videoButton.navigation.selectOnLeft;
        newVideoNav.selectOnRight = videoButton.navigation.selectOnRight;

        Navigation newAudioNav = new Navigation();
        newAudioNav.mode = Navigation.Mode.Explicit;
        newAudioNav.selectOnUp = audioButton.navigation.selectOnUp;
        newAudioNav.selectOnLeft = audioButton.navigation.selectOnLeft;
        newAudioNav.selectOnRight = audioButton.navigation.selectOnRight;

        switch (tab)
        {
            case Tab.Video:
#if UNITY_SWITCH
                newApplyNav.selectOnUp = videoButton;
                newResetNav.selectOnUp = videoButton;
                newVideoNav.selectOnDown = resetButton;
                newAudioNav.selectOnDown = applyButton;
#else
                newApplyNav.selectOnUp = graphicsQualityDropdown;
                newResetNav.selectOnUp = graphicsQualityDropdown;
                newVideoNav.selectOnDown = fullScreenToggle;
                newAudioNav.selectOnDown = fullScreenToggle;
#endif
                break;
            case Tab.Audio:
                newApplyNav.selectOnUp = musicSlider;
                newResetNav.selectOnUp = musicSlider;
                newVideoNav.selectOnDown = masterSlider;
                newAudioNav.selectOnDown = masterSlider;
                break;
            case Tab.Game:
                newApplyNav.selectOnUp = videoButton;
                newResetNav.selectOnUp = videoButton;
                newVideoNav.selectOnDown = applyButton;
                newAudioNav.selectOnDown = applyButton;
                break;
            default:
                newApplyNav.selectOnUp = videoButton;
                newResetNav.selectOnUp = videoButton;
                newVideoNav.selectOnDown = applyButton;
                newAudioNav.selectOnDown = applyButton;
                break;
        }
        applyButton.navigation = newApplyNav;
        resetButton.navigation = newResetNav;
        videoButton.navigation = newVideoNav;
        audioButton.navigation = newAudioNav;
    }

    public void SetSelected()
    {
        EventSystem.current.SetSelectedGameObject(selectedObject);
    }

    private void LoadSavedIntoPending()
    {
        pendingMaster = PlayerPrefs.GetFloat(SettingsInitialiser.MasterVolKey, DEFAULT_MASTER);
        pendingSfx = PlayerPrefs.GetFloat(SettingsInitialiser.SfxVolKey, DEFAULT_SFX);
        pendingMusic = PlayerPrefs.GetFloat(SettingsInitialiser.MusicVolKey, DEFAULT_MUSIC);

        pendingFullscreen = PlayerPrefs.GetInt("Fullscreen", DEFAULT_FULLSCREEN ? 1 : 0) == 1;
        pendingResolution = PlayerPrefs.GetInt("ResolutionLevel", DEFAULT_RESOLUTION);
        pendingQuality = PlayerPrefs.GetInt("QualityLevel", DEFAULT_QUALITY);
    }

    private void ApplyPendingToUI()
    {
        masterSlider.value = pendingMaster * 100f;
        sfxSlider.value = pendingSfx * 100f;
        musicSlider.value = pendingMusic * 100f;

        masterValueText.text = Mathf.RoundToInt(masterSlider.value).ToString();
        sfxValueText.text = Mathf.RoundToInt(sfxSlider.value).ToString();
        musicValueText.text = Mathf.RoundToInt(musicSlider.value).ToString();

        fullScreenToggle.isOn = pendingFullscreen;

        resolutionDropdown.value = pendingResolution;
        resolutionDropdown.RefreshShownValue();

        graphicsQualityDropdown.value = pendingQuality;
        graphicsQualityDropdown.RefreshShownValue();
    }

    private void UpdateApplyButton()
    {
        bool hasChanges =
            !Mathf.Approximately(
                pendingMaster,
                PlayerPrefs.GetFloat(SettingsInitialiser.MasterVolKey, DEFAULT_MASTER)
            )
            ||
            !Mathf.Approximately(
                pendingSfx,
                PlayerPrefs.GetFloat(SettingsInitialiser.SfxVolKey, DEFAULT_SFX)
            )
            ||
            !Mathf.Approximately(
                pendingMusic,
                PlayerPrefs.GetFloat(SettingsInitialiser.MusicVolKey, DEFAULT_MUSIC)
            )
            ||
            pendingFullscreen !=
            (PlayerPrefs.GetInt("Fullscreen", DEFAULT_FULLSCREEN ? 1 : 0) == 1)
            ||
            pendingResolution !=
            PlayerPrefs.GetInt("ResolutionLevel", DEFAULT_RESOLUTION)
            ||
            pendingQuality !=
            PlayerPrefs.GetInt("QualityLevel", DEFAULT_QUALITY);

        applyButton.interactable = hasChanges;
    }

    public void ApplySettings()
    {
        PlayerPrefs.SetFloat(SettingsInitialiser.MasterVolKey, pendingMaster);
        PlayerPrefs.SetFloat(SettingsInitialiser.SfxVolKey, pendingSfx);
        PlayerPrefs.SetFloat(SettingsInitialiser.MusicVolKey, pendingMusic);

        PlayerPrefs.SetInt("Fullscreen", pendingFullscreen ? 1 : 0);
        PlayerPrefs.SetInt("ResolutionLevel", pendingResolution);
        PlayerPrefs.SetInt("QualityLevel", pendingQuality);

        SaveManager.Save();

        EventSystem.current.SetSelectedGameObject(resetButton.gameObject);
        UpdateApplyButton();
    }

    public void CancelSettings()
    {
        LoadSavedIntoPending();

        ApplyPendingToUI();
        ApplyVideoRuntime();
        ApplyAudioRuntime();

        UpdateApplyButton();
    }

    public void ResetSettings()
    {
        pendingMaster = DEFAULT_MASTER;
        pendingSfx = DEFAULT_SFX;
        pendingMusic = DEFAULT_MUSIC;

        pendingFullscreen = DEFAULT_FULLSCREEN;
        pendingResolution = DEFAULT_RESOLUTION;
        pendingQuality = DEFAULT_QUALITY;

        ApplyPendingToUI();
        ApplyVideoRuntime();
        ApplyAudioRuntime();

        ApplySettings();

        UpdateApplyButton();
    }

    private void ApplyVideoRuntime()
    {
        Screen.fullScreen = pendingFullscreen;

        Screen.SetResolution(
            resolutionsWidth[pendingResolution],
            resolutionsHeight[pendingResolution],
            pendingFullscreen
        );

        QualitySettings.SetQualityLevel(pendingQuality);
        Application.targetFrameRate = pendingQuality == 0 ? 60 : -1;
    }

    private void ApplyAudioRuntime()
    {
        masterVCA.setVolume(pendingMaster);
        sfxVCA.setVolume(pendingSfx);
        musicVCA.setVolume(pendingMusic);
    }

    public void OpenVideoTab() => SetTab(Tab.Video, false);
    public void OpenAudioTab() => SetTab(Tab.Audio, false);
    public void OpenGameTab() => SetTab(Tab.Game, false);
}