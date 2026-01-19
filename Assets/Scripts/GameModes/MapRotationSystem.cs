using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq; 

public class MapRotationSystem : MonoBehaviour
{
    [SerializeField] private MapSettingsSO mapSetting;
    [SerializeField] private int maxRounds = 3;
    public int MaxRounds {get => maxRounds;}
    
    [SerializeField] private MapSettingsSO[] mapSettings;

    public static MapRotationSystem Instance;
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
        if (roundCount < maxRounds)
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

        MapSettingsSO chosenMap = availableMaps[Random.Range(0, availableMaps.Count)];
        chosenMap.PlayedThisLoop = true;

        NetworkManager.Singleton.SceneManager.LoadScene(
            chosenMap.SceneName,
            LoadSceneMode.Single
        );

        return true;
    }
}
