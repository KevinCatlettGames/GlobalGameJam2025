using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode; 

public class LevelSwitcher : MonoBehaviour
{
    public string[] scenes; 
    
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            NetworkManager.Singleton.SceneManager.LoadScene(scenes[0], LoadSceneMode.Single);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            NetworkManager.Singleton.SceneManager.LoadScene(scenes[1], LoadSceneMode.Single);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            NetworkManager.Singleton.SceneManager.LoadScene(scenes[2], LoadSceneMode.Single);
        }
        else if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            NetworkManager.Singleton.SceneManager.LoadScene(scenes[3], LoadSceneMode.Single);
        }
    }
}