using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class StartServerIfNotActive : MonoBehaviour
{
    [SerializeField] PlayerInputManager playerInputManager;

    private void Awake()
    {
        if (NetworkManager.Singleton && NetworkManager.Singleton.gameObject != gameObject)
            Destroy(gameObject);
    }
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (NetworkManager.Singleton.ConnectedClients.Count <= 0)
        {
            playerInputManager.enabled = true; 
            NetworkManager.Singleton.StartHost();
            ItemSpawner.Instance.InitialSpawn();
        }
        else
        {
            enabled = false; 
        }
    }
}