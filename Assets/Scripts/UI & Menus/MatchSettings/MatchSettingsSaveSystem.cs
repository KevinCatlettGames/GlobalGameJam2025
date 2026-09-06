using Unity.Netcode;
using UnityEngine;

public class MatchSettingsSaveSystem : MonoBehaviour
{
    public static MatchSettingsSaveSystem instance;
    [SerializeField] MatchSettingsUISetup matchSettingsUISetup;
    private void Awake()
    {
        if (instance == null)
            instance = this;
        else
            Destroy(gameObject);
    }

    private void OnEnable()
    {
        if(TransportSwitcher.Instance && !TransportSwitcher.Instance.isUsingRelay || NetworkManager.Singleton.IsServer)
            Invoke(nameof(Load), .2f);
    }

    private void Load()
    {
        LoadMatchSettings();
    }

    public void SaveMatchSettings()
    {
        LobbyManager lobbyManager = LobbyManager.instance;

        // GENERAL
        PlayerPrefs.SetInt("GameMode", (int)lobbyManager.SelectedGameMode);
        PlayerPrefs.SetInt("LoadOutType", (int)lobbyManager.selectedLoadoutType);
        PlayerPrefs.SetInt("LeftSpellIndex", lobbyManager.selectedLeftSpellIndex);
        PlayerPrefs.SetInt("RightSpellIndex", lobbyManager.selectedRightSpellIndex);
        PlayerPrefs.SetInt("WinsNeeded", lobbyManager.winsNeeded);
        PlayerPrefs.SetInt("PlayTutorial", lobbyManager.playTutorial ? 1 : 0);
        PlayerPrefs.SetInt("PlayEndless", lobbyManager.playEndless ? 1 : 0);

        // SPELLS
        for (int i = 0; i < lobbyManager.Spells.Length; i++)
        {
            PlayerPrefs.SetInt($"Spell_{i}_CanUse", lobbyManager.Spells[i].CanUse ? 1 : 0);
        }

        // MAPS
        for (int i = 0; i < lobbyManager.MapSettings.Length; i++)
        {
            PlayerPrefs.SetInt($"Map_{i}_Play", lobbyManager.MapSettings[i].PlayMap ? 1 : 0);
            PlayerPrefs.SetInt($"Map_{i}_Event", lobbyManager.MapSettings[i].PlayWithMapEvent ? 1 : 0);
            PlayerPrefs.SetInt($"Map_{i}_Rounds", lobbyManager.MapSettings[i].MapRounds);
        }

        PlayerPrefs.Save();
    }

    public void LoadMatchSettings()
    {
        LoadGeneralSettings();
        LoadSpellSettings();
        LoadMapSettings();
    }

    public void ResetToDefaults()
    {
        LobbyManager lobbyManager = LobbyManager.instance;

        PlayerPrefs.DeleteKey("GameMode");
        PlayerPrefs.DeleteKey("LoadOutType");
        PlayerPrefs.DeleteKey("LeftSpellIndex");
        PlayerPrefs.DeleteKey("RightSpellIndex");
        PlayerPrefs.DeleteKey("WinsNeeded");
        PlayerPrefs.DeleteKey("PlayTutorial");
        PlayerPrefs.DeleteKey("PlayEndless");

        for (int i = 0; i < lobbyManager.Spells.Length; i++)
        {
            PlayerPrefs.DeleteKey($"Spell_{i}_CanUse");
        }

        for (int i = 0; i < lobbyManager.MapSettings.Length; i++)
        {
            PlayerPrefs.DeleteKey($"Map_{i}_Play");
            PlayerPrefs.DeleteKey($"Map_{i}_Event");
            PlayerPrefs.DeleteKey($"Map_{i}_Rounds");
        }

        PlayerPrefs.Save();

        if (lobbyManager != null)
            lobbyManager.ResetToDefaultSettings();

        if(matchSettingsUISetup)
            matchSettingsUISetup.Setup();
    }

    private void LoadGeneralSettings()
    {
        LobbyManager lobbyManager = LobbyManager.instance;

        lobbyManager.SelectedGameMode = (GameManager.GameModeType)PlayerPrefs.GetInt("GameMode", (int)lobbyManager.SelectedGameMode);
        lobbyManager.selectedLoadoutType = (LoadoutSelection.LoadOutType)PlayerPrefs.GetInt("LoadOutType", (int)lobbyManager.selectedLoadoutType);

        lobbyManager.selectedLeftSpellIndex = PlayerPrefs.GetInt("LeftSpellIndex", lobbyManager.selectedLeftSpellIndex);
        lobbyManager.selectedRightSpellIndex = PlayerPrefs.GetInt("RightSpellIndex", lobbyManager.selectedRightSpellIndex);
        lobbyManager.winsNeeded = PlayerPrefs.GetInt("WinsNeeded", lobbyManager.winsNeeded);

        lobbyManager.playTutorial = PlayerPrefs.GetInt("PlayTutorial", lobbyManager.playTutorial ? 1 : 0) == 1;
        lobbyManager.playEndless = PlayerPrefs.GetInt("PlayEndless", lobbyManager.playEndless ? 1 : 0) == 1;
    }

    private void LoadSpellSettings()
    {
        LobbyManager lobbyManager = LobbyManager.instance;

        for (int i = 0; i < lobbyManager.Spells.Length; i++)
        {
            int defaultCanUse = lobbyManager.Spells[i].CanUse ? 1 : 0;
            lobbyManager.Spells[i].CanUse = PlayerPrefs.GetInt($"Spell_{i}_CanUse", defaultCanUse) == 1;
        }
    }

    private void LoadMapSettings()
    {
        LobbyManager lobbyManager = LobbyManager.instance;

        for (int i = 0; i < lobbyManager.MapSettings.Length; i++)
        {
            int defaultPlay = lobbyManager.MapSettings[i].PlayMap ? 1 : 0;
            int defaultEvent = lobbyManager.MapSettings[i].PlayWithMapEvent ? 1 : 0;

            lobbyManager.MapSettings[i].PlayMap = PlayerPrefs.GetInt($"Map_{i}_Play", defaultPlay) == 1;
            lobbyManager.MapSettings[i].PlayWithMapEvent = PlayerPrefs.GetInt($"Map_{i}_Event", defaultEvent) == 1;
            lobbyManager.MapSettings[i].MapRounds = PlayerPrefs.GetInt($"Map_{i}_Rounds", lobbyManager.MapSettings[i].MapRounds);
        }
    }
}