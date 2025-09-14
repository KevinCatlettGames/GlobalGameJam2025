using Unity.Netcode;
using UnityEditor.Build.Player;
using UnityEngine;
using UnityEngine.InputSystem;

public class StartServerIfNotActive : MonoBehaviour
{
    [SerializeField] PlayerInputManager playerInputManager;
    
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