using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    private FMOD.Studio.VCA masterVCA;
    private FMOD.Studio.VCA sfxVCA;
    private FMOD.Studio.VCA musicVCA;
    
    [SerializeField] private Toggle fullScreenToggle;
    [SerializeField] private TMP_Dropdown resolutionDropdown;
    [SerializeField] private TMP_Dropdown graphicsQualityDropdown;
    [SerializeField] private int[] resolutionsWidth;
    [SerializeField] private int[] resolutionsHeight;

    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider musicSlider;

    [SerializeField] private TextMeshProUGUI masterValueText;
    [SerializeField] private TextMeshProUGUI sfxValueText;
    [SerializeField] private TextMeshProUGUI musicValueText;

    [SerializeField] private GameObject selecedObject;

    private float masterVolume;
    private float sfxVolume;
    private float musicVolume;

    void Start()
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


        int value = Mathf.CeilToInt(PlayerPrefs.GetFloat(SettingsInitialiser.MasterVolKey) * 100);
        masterValueText.text = value.ToString();
        value = Mathf.CeilToInt(PlayerPrefs.GetFloat(SettingsInitialiser.SfxVolKey) * 100);
        sfxValueText.text = value.ToString();
        value = Mathf.CeilToInt(PlayerPrefs.GetFloat(SettingsInitialiser.MusicVolKey) * 100);
        musicValueText.text = value.ToString();

        fullScreenToggle.isOn = Screen.fullScreen;
        //resolutionDropdown.interactable = !Screen.fullScreen;
        InitialiseResolutions();
        graphicsQualityDropdown.value = QualitySettings.GetQualityLevel();
        graphicsQualityDropdown.RefreshShownValue();
    }
    public void SetMasterVolume(float volume)
    { 
        masterValueText.text = volume.ToString();
        volume *= .01f;
        masterVCA.setVolume(volume);
        PlayerPrefs.SetFloat(SettingsInitialiser.MasterVolKey, volume);
    }
    public void SetSFXVolume(float volume)
    {
        sfxValueText.text = volume.ToString();
        volume *= .01f;
        sfxVCA.setVolume(volume);
        PlayerPrefs.SetFloat(SettingsInitialiser.SfxVolKey, volume);
    }
    public void SetMusicVolume(float volume)
    {
        musicValueText.text = volume.ToString();
        volume *= .01f;
        musicVCA.setVolume(volume);
        PlayerPrefs.SetFloat(SettingsInitialiser.MusicVolKey, volume);
    }

    public void SetFullscreen(bool isFullScreen)
    {
        Screen.fullScreen = isFullScreen;
        int fullscreen = isFullScreen ? 1 : 0;
        PlayerPrefs.SetInt("Fullscreen", fullscreen);
        resolutionDropdown.interactable = !isFullScreen;
    }
    public void SetSelected()
    {
        EventSystem eventSystem = EventSystem.current;
        eventSystem.SetSelectedGameObject(selecedObject);
    }
    public void SetResolution(int option)
    {
        if (resolutionsWidth.Length != resolutionsHeight.Length)
        {
            Debug.Log("Resolution Arrays dont match");
            return;
        }
        Screen.SetResolution(resolutionsWidth[option], resolutionsHeight[option], Screen.fullScreen);
        PlayerPrefs.SetInt("ResolutionLevel", option);
    }
    public void SetGraphicsQuality(int option)
    {
        QualitySettings.SetQualityLevel(option);
        PlayerPrefs.SetInt("QualityLevel", option);
    }
    private void InitialiseResolutions()
    {
        if (resolutionsWidth.Length != resolutionsHeight .Length)
        {
            Debug.Log("Resolution Arrays dont match");
            return;
        }
        resolutionDropdown.ClearOptions();

        List<string> resolutionLables = new List<string>();

        for (int i = 0; i < resolutionsWidth.Length; i++)
        {
            string lable = resolutionsWidth[i] + " x " + resolutionsHeight[i];
            resolutionLables.Add(lable);
        }
        resolutionDropdown.AddOptions(resolutionLables);
        int value = PlayerPrefs.GetInt("ResolutionLevel", 2);
        resolutionDropdown.value = value;
        resolutionDropdown.RefreshShownValue();
        resolutionDropdown.interactable = !Screen.fullScreen;
    }
}
