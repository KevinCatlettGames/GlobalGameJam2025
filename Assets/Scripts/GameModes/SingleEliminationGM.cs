using System.Collections;
using Unity.VisualScripting;
using System.Collections;
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
    
    public override void CheckForRoundEndLocal()
    {
        int alivePlayers = 0;
        for (int i = 0; i < playerStates.Length; i++)
        {
            if (playerStates[i] == PlayerState.alive)
            {
                alivePlayers++;
            }
        }
        
        Debug.Log(alivePlayers);
        if (alivePlayers <= 1)
        {
            CallGameEndLocal();
        }
    }

    [ClientRpc]
    void CallGameEndClientRpc()
    {
        Debug.Log("GameEnded");
        StartCoroutine(AwardVictory());
    }

    void CallGameEndLocal()
    {
        Debug.Log("GameEnded");
        StartCoroutine(AwardVictory());
    }
    
    private void Update()
    {
        if (gameEnded && (Input.GetKeyDown(KeyCode.JoystickButton7) || Input.GetKeyDown(KeyCode.Return)))
        {
            RestartGame();
        }
    }

    private IEnumerator AwardVictory()
    {
        yield return new WaitForSeconds(gameEndDelay);
        int winnerID = -1;
        for (int i = 0; i < playerStates.Length; i++)
        {
            if (playerStates[i] == PlayerState.alive)
            {
                winnerID = i;
                players[winnerID].Victory();
                break;
            }
        }
        victoryAnimator.gameObject.SetActive(true);
        switch (winnerID)
        {
            case 0:
                victoryAnimator.Play("P0");
                break;
            case 1:
                victoryAnimator.Play("P1");
                break;
            case 2:
                victoryAnimator.Play("P2");
                break;
            case 3:
                victoryAnimator.Play("P3");
                break;
            default:
                victoryAnimator.gameObject.SetActive(false);
                EndGame();
                yield break;
        }
        yield return new WaitForSeconds(1f);
        playerHUDs[winnerID].AddWin();
        victoryAnimator.gameObject.SetActive(false);
        yield return new WaitForSeconds(.75f);
        base.EndGame();
    }

}
