using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq; 

public class MapRotationSystem : MonoBehaviour
{
    [SerializeField] private MapSettingsSO mapSetting;
    [SerializeField] private int maxRounds = 3;
    [SerializeField] private bool enableMapSwitch = true;
    public int MaxRounds {get => maxRounds;}
    
    [SerializeField] private MapSettingsSO[] mapSettings;

    public static MapRotationSystem Instance;
    MapSettingsSO chosenMap;
    
    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        if(mapSetting) 
            maxRounds = mapSetting.MapRounds;
    }

    public bool CheckForMapSwitch(int roundCount)
    {
        if (!enableMapSwitch || roundCount < maxRounds || SteamIntegration.instance && !SteamIntegration.instance.IsFullVersion)
            return false;

        string currentScene = SceneManager.GetActiveScene().name;
        
        List<MapSettingsSO> availableMaps = mapSettings
            .Where(m => m.PlayMap && !m.PlayedThisLoop && m.SceneName != currentScene)
            .ToList();
        
        if (availableMaps.Count == 0)
        {
            foreach (var map in mapSettings)
                map.PlayedThisLoop = false;

            availableMaps = mapSettings
                .Where(m => m.PlayMap && m.SceneName != currentScene)
                .ToList();
        }

        if (availableMaps.Count == 0)
            return false;

        chosenMap = availableMaps[Random.Range(0, availableMaps.Count)];
        chosenMap.PlayedThisLoop = true;

        LoadMap();

        return true;
    }

    public void LoadMap()
    {
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && !UNITY_SWITCH
        NetworkManager.Singleton.SceneManager.LoadScene(
            chosenMap.SceneName,
            LoadSceneMode.Single
        );
#else
        SceneManager.LoadScene(chosenMap.SceneName);
#endif
    }
}