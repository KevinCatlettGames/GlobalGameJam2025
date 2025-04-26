using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class TestingNetcodeUI : NetworkBehaviour
{
    [SerializeField] private Button startHostButton;
    [SerializeField] private Button startClientButton;
    [SerializeField] private Button startGameButton;

    private void Awake()
    {
        Time.timeScale = 0;

        startHostButton.onClick.AddListener(() =>
        {
            Debug.Log("Start host");
            NetworkManager.Singleton.StartHost();
            
            // Host acts as server and client at the same time
            Hide(); 

            startGameButton.gameObject.SetActive(true);

            startGameButton.onClick.AddListener(() =>
            {
                Debug.Log("Start game");

                // Tell connected clients to also hide UI
                StartGameClientRpc();
            });

            // Also listen for future clients connecting
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
        });

        startClientButton.onClick.AddListener(() =>
        {
            Debug.Log("Start client");
            NetworkManager.Singleton.StartClient();
        });
    }

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Client connected with ID: {clientId}");

        // When clients connect, they are shown the start UI (until host tells them to hide it)
        // No action needed here unless you want to auto-hide
    }

    [ClientRpc]
    private void StartGameClientRpc()
    {
        // Example: Call your player manager to initialize stuff
        PlayerManager.Instance.InitializeCallClientRpc();
        // Resume game
        Time.timeScale = 1;
        Hide();
        startGameButton.gameObject.SetActive(false);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}