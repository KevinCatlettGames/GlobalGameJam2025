using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class Settings : MonoBehaviour
{
    private FMOD.Studio.VCA masterVCA;
    private FMOD.Studio.VCA sfxVCA;
    private FMOD.Studio.VCA musicVCA;
    
    [SerializeField] private Toggle fullScreenToggle;

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

        int value = (int)(masterVolume * 100);
        masterValueText.text = value.ToString();
        value = (int)(sfxVolume * 100);
        sfxValueText.text = value.ToString();
        value = (int)(musicVolume * 100);
        musicValueText.text = value.ToString();

        fullScreenToggle.isOn = Screen.fullScreen;
    }
    public void SetMasterVolume(float volume)
    {
        
        masterValueText.text = volume.ToString();
        volume *= .01f;
        masterVCA.setVolume(volume);
        //masterVolume = volume;
    }
    public void SetSFXVolume(float volume)
    {
        sfxValueText.text = volume.ToString();
        volume *= .01f;
        sfxVCA.setVolume(volume);
        //masterVolume = volume;
    }
    public void SetMusicVolume(float volume)
    {
        musicValueText.text = volume.ToString();
        volume *= .01f;
        musicVCA.setVolume(volume);
        //masterVolume = volume;
    }
    public void SetSelected()
    {
        EventSystem eventSystem = EventSystem.current;
        eventSystem.SetSelectedGameObject(selecedObject);
    }

    public void SetFullscreen(bool isFullScreen)
    {
        Screen.fullScreen = isFullScreen;
    }
}
