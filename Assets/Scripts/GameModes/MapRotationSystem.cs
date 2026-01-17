using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
public class MapRotationSystem : MonoBehaviour
{
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
    }

    public bool CheckForMapSwitch(int roundCount)
    {
        List<string> sceneNames  = new List<string>();
        foreach (MapSettingsSO mapSetting in mapSettings)
        {
            if(mapSetting.PlayMap)
                sceneNames.Add(mapSetting.SceneName);
        }

        if (roundCount < maxRounds || sceneNames.Count <= 1)
            return false; 

        string i;
        do
        {
            int r = Random.Range(0, sceneNames.Count);
            i = sceneNames[r];
        }
        while (i == SceneManager.GetActiveScene().name);
        NetworkManager.Singleton.SceneManager.LoadScene(i, LoadSceneMode.Single);
        return true;
    }
}
