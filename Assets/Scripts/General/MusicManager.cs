using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MusicManager : MonoBehaviour
{
    public static MusicManager instance;

    [SerializeField] private string[] scenesToPersistIn;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        SceneManager.sceneLoaded += SceneManagerOnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= SceneManagerOnSceneLoaded;
    }

    private void SceneManagerOnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        bool shouldPersist = false;

        foreach (string sceneName in scenesToPersistIn)
        {
            if (sceneName == scene.name)
            {
                shouldPersist = true;
                break;
            }
        }

        if (!shouldPersist)
            Destroy(gameObject);
    }
}