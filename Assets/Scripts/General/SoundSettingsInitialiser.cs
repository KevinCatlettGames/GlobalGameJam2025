using UnityEngine;

public class SoundSettingsInitialiser : MonoBehaviour
{
    public static string masterVolKey = "masterVoulume";
    public static string sfxVolKey = "sfxVoulume";
    public static string musicVolKey = "musicVoulume";

    [SerializeField] private float defaultValue = .5f;
    [SerializeField] private bool resetAllPlayerPrefs = false;

    private void Awake()
    {
        if (resetAllPlayerPrefs) PlayerPrefs.DeleteAll();
    }
    void Start()
    {
        FMOD.Studio.VCA masterVCA = FMODUnity.RuntimeManager.GetVCA("vca:/Master");
        FMOD.Studio.VCA sfxVCA = FMODUnity.RuntimeManager.GetVCA("vca:/SFX");
        FMOD.Studio.VCA musicVCA = FMODUnity.RuntimeManager.GetVCA("vca:/Music");

        masterVCA.setVolume(PlayerPrefs.GetFloat(masterVolKey, defaultValue));
        sfxVCA.setVolume(PlayerPrefs.GetFloat(sfxVolKey, defaultValue));
        musicVCA.setVolume(PlayerPrefs.GetFloat(sfxVolKey,defaultValue));
    }
}
