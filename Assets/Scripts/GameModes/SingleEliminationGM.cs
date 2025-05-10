using Unity.VisualScripting;
using UnityEngine;
using Unity.Netcode; 

public class SingleEliminationGM : GameManager
{
    [ServerRpc(RequireOwnership = false)]
    public override void CheckForRoundEndServerRpc()
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
           CallGameEndClientRpc();
        }
    }

    [ClientRpc]
    void CallGameEndClientRpc()
    {
        Debug.Log("GameEnded");
        Invoke(nameof(EndGame), gameEndDelay);
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
                playerHUDs[i].AddWin();
                Debug.Log("Player " + i + " Victory");
            }
        }
        base.EndGame();
    }
}
