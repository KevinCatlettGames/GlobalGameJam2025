using UnityEngine;
using UnityEngine.UI;

public class AudioSettings : MonoBehaviour
{
    private FMOD.Studio.VCA masterVCA;
    private FMOD.Studio.VCA sfxVCA;
    private FMOD.Studio.VCA musicVCA;

    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider sfxSlider;
    [SerializeField] private Slider musicSlider;

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
    }
    public void SetMasterVolume(float volume)
    {
        masterVCA.setVolume(volume);
        Debug.Log(volume);
        //masterVolume = volume;
    }
    public void SetSFXVolume(float volume)
    {
        sfxVCA.setVolume(volume);
        //masterVolume = volume;
    }
    public void SetMusicVolume(float volume)
    {
        musicVCA.setVolume(volume);
        //masterVolume = volume;
    }


}
