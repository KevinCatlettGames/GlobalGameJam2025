using UnityEngine;

public class SettingsInitialiser : MonoBehaviour
{
    public static string MasterVolKey = "masterVoulume";
    public static string SfxVolKey = "sfxVoulume";
    public static string MusicVolKey = "musicVoulume";

    [SerializeField] private float defaultValue = .5f;
    [SerializeField] private bool resetAllPlayerPrefs = false;

    private void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;

        if (resetAllPlayerPrefs)
        {
            PlayerPrefs.DeleteAll();
            SaveManager.Save();
        }

        SaveManager.Initialize();

        Invoke(nameof(Set), .1f);
    }

    void Set()
    {
        FMOD.Studio.VCA masterVCA = FMODUnity.RuntimeManager.GetVCA("vca:/Master");
        FMOD.Studio.VCA sfxVCA = FMODUnity.RuntimeManager.GetVCA("vca:/SFX");
        FMOD.Studio.VCA musicVCA = FMODUnity.RuntimeManager.GetVCA("vca:/Music");

        //Debug.Log(PlayerPrefs.GetFloat(masterVolKey));
        //Debug.Log(PlayerPrefs.GetFloat(sfxVolKey));
        //Debug.Log(PlayerPrefs.GetFloat(musicVolKey));

        masterVCA.setVolume(PlayerPrefs.GetFloat(MasterVolKey, defaultValue));
        sfxVCA.setVolume(PlayerPrefs.GetFloat(SfxVolKey, defaultValue));
        musicVCA.setVolume(PlayerPrefs.GetFloat(MusicVolKey, defaultValue));

        int fullScreen = PlayerPrefs.GetInt("Fullscreen", 1);
        if (fullScreen == 1)
        {
            Screen.fullScreen = true;
        }
        else
        {
            Screen.fullScreen = false;
        }

        int quality = PlayerPrefs.GetInt("QualityLevel", 2);
        QualitySettings.SetQualityLevel(quality);
        if (quality == 0)
            Application.targetFrameRate = 60;
        else
            Application.targetFrameRate = -1;
    }
}