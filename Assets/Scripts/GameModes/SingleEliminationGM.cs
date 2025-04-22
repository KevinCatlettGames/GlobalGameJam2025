using Unity.VisualScripting;
using UnityEngine;

public class SingleEliminationGM : GameManager
{
    public override void CheckForRoundEnd()
    {
        int alivePlayers = 0;
        for (int i = 0; i < playerStates.Length; i++)
        {
            if (playerStates[i] == PlayerState.alive)
            {
                alivePlayers++;
            }
        }
        if (alivePlayers <= 1)
        {
            Debug.Log("GameEnded");
            Invoke(nameof(EndGame), gameEndDelay);
        }
    }
    private void Update()
    {
        if (gameEnded && (Input.GetKeyDown(KeyCode.JoystickButton7) || Input.GetKeyDown(KeyCode.Return)))
        {
            RestartGame();
        }
    }

    public override void EndGame()
    {
        for (int i = 0; i < playerStates.Length; i++)
        {
            if (playerStates[i] == PlayerState.alive)
            {
                players[i].Victory();
                Debug.Log("Player " + i + " Victory");
            }
        }
        base.EndGame();
    }
}
