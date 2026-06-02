using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class StartServerIfNotActive : MonoBehaviour
{
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && !UNITY_SWITCH

    [SerializeField] PlayerInputManager playerInputManager;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (LobbyManager.instance != null || playerInputManager != null)
            return; 

        if (playerInputManager == null)
            playerInputManager = PlayerManager.Instance.GetComponent<PlayerInputManager>();
        playerInputManager.enabled = true;
        NetworkManager.Singleton.StartHost();
        ItemSpawner.Instance.InitialSpawn();
    }
#endif
}