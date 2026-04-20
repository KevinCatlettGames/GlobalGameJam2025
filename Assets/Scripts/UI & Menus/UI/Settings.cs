using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class Settings : MonoBehaviour
{
    #region Defaults
    private const float DEFAULT_MASTER = 1f;
    private const float DEFAULT_SFX = 1f;
    private const float DEFAULT_MUSIC = 1f;

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

    private float masterVolume;
    private float sfxVolume;
    private float musicVolume;

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
    #endregion

    #region Tab UI
    [Header("Tab UI")]
    [SerializeField] private Color activeColor;
    [SerializeField] private Color inactiveColor;
    [SerializeField] private GameObject videoTabFrame;
    [SerializeField] private GameObject audioTabFrame;
    [SerializeField] private GameObject gameTabFrame;
    [SerializeField] private GameObject lbFrame;
    [SerializeField] private GameObject rbFrame;
    #endregion

    #region UI
    [Header("UI")]
    [SerializeField] private GameObject selectedObject;
    #endregion

    #region Enums
    public enum Tab { Video, Audio, Game }
    #endregion

    #region Tab State
    public Tab currentTab = Tab.Video;
    private bool tabTogglingEnabled;
    #endregion

    #region Unity Lifecycle
    private void OnEnable()
    {
        exitSettingsAction.action.performed += ExitSettings;
        exitSettingsAction.action.Enable();

        LoadSavedIntoPending();
        ApplyPendingToUI();
        ApplyVideoRuntime();
        ApplyAudioRuntime();
        SetTab(Tab.Video);
    }

    private void OnDisable()
    {
        exitSettingsAction.action.performed -= ExitSettings;
        exitSettingsAction.action.Disable();
        DisableTabToggling();
    }

    private void Start()
    {
        InitialiseAudio();
        InitialiseVideo();
    }
    #endregion

    #region Initialisation
    private void InitialiseAudio()
    {
        masterVCA = FMODUnity.RuntimeManager.GetVCA("vca:/Master");
        sfxVCA = FMODUnity.RuntimeManager.GetVCA("vca:/SFX");
        musicVCA = FMODUnity.RuntimeManager.GetVCA("vca:/Music");

        masterVCA.getVolume(out masterVolume);
        sfxVCA.getVolume(out sfxVolume);
        musicVCA.getVolume(out musicVolume);

        masterSlider.value = masterVolume * 100;
        sfxSlider.value = sfxVolume * 100;
        musicSlider.value = musicVolume * 100;

        masterValueText.text = Mathf.CeilToInt(PlayerPrefs.GetFloat(SettingsInitialiser.MasterVolKey) * 100).ToString();
        sfxValueText.text = Mathf.CeilToInt(PlayerPrefs.GetFloat(SettingsInitialiser.SfxVolKey) * 100).ToString();
        musicValueText.text = Mathf.CeilToInt(PlayerPrefs.GetFloat(SettingsInitialiser.MusicVolKey) * 100).ToString();

        pendingMaster = masterVolume;
        pendingSfx = sfxVolume;
        pendingMusic = musicVolume;
    }

    private void InitialiseVideo()
    {
        fullScreenToggle.isOn = Screen.fullScreen;
        InitialiseResolutions();

        graphicsQualityDropdown.value = QualitySettings.GetQualityLevel();
        graphicsQualityDropdown.RefreshShownValue();

        pendingFullscreen = Screen.fullScreen;
        pendingResolution = PlayerPrefs.GetInt("ResolutionLevel", 2);
        pendingQuality = QualitySettings.GetQualityLevel();
    }
    #endregion

    #region Video Controls
    public void SetFullscreen(bool isFullScreen)
    {
        pendingFullscreen = isFullScreen;

        Screen.fullScreen = isFullScreen;
        resolutionDropdown.interactable = !isFullScreen;
        ApplyVideoRuntime();
    }

    public void SetResolution(int option)
    {
        pendingResolution = option;

        Screen.SetResolution(
            resolutionsWidth[option],
            resolutionsHeight[option],
            pendingFullscreen
        );
        ApplyVideoRuntime();
    }

    public void SetGraphicsQuality(int option)
    {
        pendingQuality = option;

        QualitySettings.SetQualityLevel(option);
        Application.targetFrameRate = option == 0 ? 60 : -1;
        ApplyVideoRuntime();
    }

    private void InitialiseResolutions()
    {
        if (resolutionsWidth.Length != resolutionsHeight.Length) return;

        resolutionDropdown.ClearOptions();

        List<string> labels = new();

        for (int i = 0; i < resolutionsWidth.Length; i++)
            labels.Add($"{resolutionsWidth[i]} x {resolutionsHeight[i]}");

        resolutionDropdown.AddOptions(labels);

        int value = PlayerPrefs.GetInt("ResolutionLevel", 2);
        resolutionDropdown.value = value;
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.interactable = !Screen.fullScreen;
    }
    #endregion

    #region Audio Controls
    public void SetMasterVolume(float volume)
    {
        masterValueText.text = volume.ToString();
        pendingMaster = volume * 0.01f;
    }

    public void SetSFXVolume(float volume)
    {
        sfxValueText.text = volume.ToString();
        pendingSfx = volume * 0.01f;
    }

    public void SetMusicVolume(float volume)
    {
        musicValueText.text = volume.ToString();
        pendingMusic = volume * 0.01f;
    }
    #endregion

    #region Input Handling
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

    private void ExitSettings(InputAction.CallbackContext obj) { }
    #endregion

    #region Tabs Logic
    public void SetTab(Tab tab)
    {
        currentTab = tab;

        if (!tabTogglingEnabled)
            EnableTabToggling();

        UpdateTabUI();
        UpdateTabVisibility(tab);
    }

    private void ChangeTab(bool forward)
    {
        if (!tabTogglingEnabled) return;

        Tab nextTab = forward
            ? (currentTab == Tab.Video ? Tab.Audio :
               currentTab == Tab.Audio ? Tab.Game : Tab.Video)
            : (currentTab == Tab.Video ? Tab.Game :
               currentTab == Tab.Game ? Tab.Audio : Tab.Video);

        SetTab(nextTab);
    }

    private void UpdateTabUI()
    {
        SetTabVisual(videoTabFrame, currentTab == Tab.Video);
        SetTabVisual(audioTabFrame, currentTab == Tab.Audio);
        SetTabVisual(gameTabFrame, currentTab == Tab.Game);
    }

    private void SetTabVisual(GameObject frame, bool active)
    {
        frame.GetComponent<Image>().color = active ? inactiveColor : activeColor;
        frame.GetComponent<Outline>().enabled = active;
    }

    private void UpdateTabVisibility(Tab tab)
    {
        videoTab.SetActive(tab == Tab.Video);
        audioTab.SetActive(tab == Tab.Audio);
        gameTab.SetActive(tab == Tab.Game);

        if (tab == Tab.Video) videoTabFrame.transform.SetAsLastSibling();
        if (tab == Tab.Audio) audioTabFrame.transform.SetAsLastSibling();
        if (tab == Tab.Game) gameTabFrame.transform.SetAsLastSibling();
    }

    public void OpenVideoTab() => SetTab(Tab.Video);
    public void OpenAudioTab() => SetTab(Tab.Audio);
    public void OpenGameTab() => SetTab(Tab.Game);
    #endregion

    #region UI Helpers
    public void SetSelected()
    {
        EventSystem.current.SetSelectedGameObject(selectedObject);
    }
    #endregion

    #region Reset And Apply
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

        resolutionDropdown.interactable = !pendingFullscreen;
    }

    public void ResetSettings()
    {
        pendingMaster = DEFAULT_MASTER;
        pendingSfx = DEFAULT_SFX;
        pendingMusic = DEFAULT_MUSIC;

        pendingFullscreen = DEFAULT_FULLSCREEN;
        pendingResolution = DEFAULT_RESOLUTION;
        pendingQuality = DEFAULT_QUALITY;

        masterVCA.setVolume(pendingMaster);
        sfxVCA.setVolume(pendingSfx);
        musicVCA.setVolume(pendingMusic);

        Screen.fullScreen = pendingFullscreen;

        Screen.SetResolution(
            resolutionsWidth[pendingResolution],
            resolutionsHeight[pendingResolution],
            pendingFullscreen
        );

        QualitySettings.SetQualityLevel(pendingQuality);
        Application.targetFrameRate = pendingQuality == 0 ? 60 : -1;

        ApplyPendingToUI();
        ApplyVideoRuntime();
        ApplyAudioRuntime();
    }

    public void ApplySettings()
    {
        masterVCA.setVolume(pendingMaster);
        sfxVCA.setVolume(pendingSfx);
        musicVCA.setVolume(pendingMusic);

        PlayerPrefs.SetFloat(SettingsInitialiser.MasterVolKey, pendingMaster);
        PlayerPrefs.SetFloat(SettingsInitialiser.SfxVolKey, pendingSfx);
        PlayerPrefs.SetFloat(SettingsInitialiser.MusicVolKey, pendingMusic);

        PlayerPrefs.SetInt("Fullscreen", pendingFullscreen ? 1 : 0);
        PlayerPrefs.SetInt("ResolutionLevel", pendingResolution);
        PlayerPrefs.SetInt("QualityLevel", pendingQuality);

        PlayerPrefs.Save();
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

    public void CancelSettings()
    {
        LoadSavedIntoPending();

        masterVCA.setVolume(pendingMaster);
        sfxVCA.setVolume(pendingSfx);
        musicVCA.setVolume(pendingMusic);

        Screen.fullScreen = pendingFullscreen;

        Screen.SetResolution(
            resolutionsWidth[pendingResolution],
            resolutionsHeight[pendingResolution],
            pendingFullscreen
        );

        QualitySettings.SetQualityLevel(pendingQuality);
        Application.targetFrameRate = pendingQuality == 0 ? 60 : -1;

        ApplyPendingToUI();
        ApplyVideoRuntime();
        ApplyAudioRuntime();
    }
    #endregion
}