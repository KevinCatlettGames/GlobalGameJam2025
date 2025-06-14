using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuMusicHandler : MonoBehaviour
{
    public static MainMenuMusicHandler Instance;
    [SerializeField] private int[] indexesToPersist;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        
        SceneManager.sceneLoaded += SceneManagerOnsceneLoaded;
        
        DontDestroyOnLoad(this.gameObject);
    }

    private void SceneManagerOnsceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        bool shouldPersist = false;

        foreach (int index in indexesToPersist)
        {
            if(index == arg0.buildIndex)
                shouldPersist = true;
        }
        
        if(!shouldPersist)
            Destroy(this.gameObject);
    }
}