using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class AudioSettings : MonoBehaviour
{
    private FMOD.Studio.VCA masterVCA;
    private FMOD.Studio.VCA sfxVCA;
    private FMOD.Studio.VCA musicVCA;

    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider musicSlider;

    [SerializeField] private TextMeshProUGUI masterValueText;
    [SerializeField] private TextMeshProUGUI sfxValueText;
    [SerializeField] private TextMeshProUGUI musicValueText;

    private float masterVolume;
    private float sfxVolume;
    private float musicVolume;

    void Start()
    {
        masterVCA = FMODUnity.RuntimeManager.GetVCA("vca:/vca_master");
        sfxVCA = FMODUnity.RuntimeManager.GetVCA("vca:/vca_sfx");
        musicVCA = FMODUnity.RuntimeManager.GetVCA("vca:/vca_music");

        masterVCA.getVolume(out masterVolume);
        sfxVCA.getVolume(out sfxVolume);
        musicVCA.getVolume(out musicVolume);

        masterSlider.value = masterVolume;
        sfxSlider.value = sfxVolume;
        musicSlider.value = musicVolume;

        int value = (int)(masterVolume * 100);
        masterValueText.text = value.ToString();
        value = (int)(sfxVolume * 100);
        sfxValueText.text = value.ToString();
        value = (int)(musicVolume * 100);
        musicValueText.text = value.ToString();
    }
    public void SetMasterVolume(float volume)
    {
        masterVCA.setVolume(volume);
        int value = (int)(volume * 100);
        masterValueText.text = value.ToString();
        //masterVolume = volume;
    }
    public void SetSFXVolume(float volume)
    {
        sfxVCA.setVolume(volume);
        int value = (int)(volume * 100);
        sfxValueText.text = value.ToString();
        //masterVolume = volume;
    }
    public void SetMusicVolume(float volume)
    {
        musicVCA.setVolume(volume);
        int value = (int)(volume * 100);
        musicValueText.text = value.ToString();
        //masterVolume = volume;
    }


}
