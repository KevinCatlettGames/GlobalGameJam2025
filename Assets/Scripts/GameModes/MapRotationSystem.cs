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
            this.enabled = false;
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

        mapSetting = chosenMap;
        maxRounds = chosenMap.MapRounds;

        if (MenuTransitionHandler.Instance && SceneManager.GetActiveScene().buildIndex == 0 || MenuTransitionHandler.Instance && SceneManager.GetActiveScene().buildIndex == 6)
        {
            MenuTransitionHandler.Instance.OnFadeComplete += LoadMap;
            MenuTransitionHandler.Instance.TriggerFade();
        }
        else
        {
            LoadMap();
        }

        return true;
    }

    public void LoadMap()
    {
        if(MenuTransitionHandler.Instance)
            MenuTransitionHandler.Instance.OnFadeComplete -= LoadMap;

        NetworkManager.Singleton.SceneManager.LoadScene(
                   chosenMap.SceneName,
                   LoadSceneMode.Single
               );
    }
}