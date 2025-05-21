using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class TestingLobbyUI : MonoBehaviour
{
    [SerializeField] private Button createGameButton;
    [SerializeField] private Button joinGameButton;
    [SerializeField] private Button startGameButton;
    [SerializeField] private TextMeshProUGUI waitingText;

    [SerializeField] private string sceneNameToLoad;
    
    private void Awake()
    {
        createGameButton.onClick.AddListener(() =>
        {
            Debug.Log("Start host");
            NetworkManager.Singleton.StartHost();
            
            startGameButton.gameObject.SetActive(true);
            startGameButton.onClick.AddListener(() =>
            {
                NetworkManager.Singleton.SceneManager.LoadScene(sceneNameToLoad, LoadSceneMode.Single);
            });
            
            createGameButton.gameObject.SetActive(false);
            joinGameButton.gameObject.SetActive(false);
        });

        joinGameButton.onClick.AddListener(() =>
        {
            Debug.Log("Start client");
            NetworkManager.Singleton.StartClient();
            createGameButton.gameObject.SetActive(false);
            joinGameButton.gameObject.SetActive(false);
            waitingText.gameObject.SetActive(true);
        });
    }
}