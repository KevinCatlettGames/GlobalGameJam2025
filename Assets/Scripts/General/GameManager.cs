using System;
using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance; 
    
    private bool gameEnded;

    public Action OnGameEnded;
    public Action OnGameStarted;

    public GameObject restartGameText; 

    

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this; 
        }
        else
        {
            Destroy(this);
        }
        Cursor.visible = false;
    }
    
    private void Update()
    {
        if (gameEnded && (Input.GetKeyDown(KeyCode.JoystickButton7) || Input.GetKeyDown(KeyCode.Return)))
        {
            RestartGame();
        }
    }

    public void EndGame()
    {
        OnGameEnded?.Invoke();
        gameEnded = true;
        restartGameText.SetActive(true);
    }

    public void RestartGame()
    {
        OnGameStarted?.Invoke();
        gameEnded = false; 
        restartGameText.SetActive(false);
    }
}
