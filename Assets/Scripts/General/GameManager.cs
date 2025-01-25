using System;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance ; 
    
    public bool gameEnded;

    public Action OnGameEnded;
    public Action OnGameStarted;

    public GameObject restartGameText; 
    private void Awake()
    {
        Instance = this; 
    }
    
    private void Update()
    {
        if (gameEnded && Input.anyKeyDown)
        {
            RestartGame();
        }
    }

    public void EndGame()
    {
        
        OnGameEnded?.Invoke();
        gameEnded = true;
        restartGameText.SetActive(true);
        Time.timeScale = 0;
    }

    public void RestartGame()
    {
        OnGameStarted?.Invoke();
        gameEnded = false; 
        restartGameText.SetActive(false);
        Time.timeScale = 1;
    }
}
