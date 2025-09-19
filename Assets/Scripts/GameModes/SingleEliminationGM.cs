using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class SingleEliminationGM : GameManager
{
    private void Start()
    {
        Time.timeScale = 1;
    }

    [ServerRpc(RequireOwnership = false)]
    public override void CheckForRoundEndServerRpc()
    {
        if (gameEnded) return;
        if (CountAlivePlayers() <= 1)
        {
            gameEnded = true;
            CallGameEndClientRpc();
        }
    }

    public override void CheckForRoundEndLocal()
    {
        if (gameEnded) return;
        if (CountAlivePlayers() <= 1)
        {
            gameEnded = true;
            CallGameEndLocal();
        }
    }

    private int CountAlivePlayers()
    {
        int count = 0;
        foreach (var state in playerStates)
        {
            if (state == PlayerState.alive || state == PlayerState.pendingRespawn) count++;
        }
        return count;
    }

    [ClientRpc]
    private void CallGameEndClientRpc()
    {
        Debug.Log("GameEnded");
        StartCoroutine(AwardVictory());
    }

    private void CallGameEndLocal()
    {
        StartCoroutine(AwardVictory());
    }

    private void Update()
    {
        if (ScoreManager.Instance.ScoresResolved && isReadyToRestart && (Input.GetKeyDown(KeyCode.JoystickButton7) || Input.GetKeyDown(KeyCode.Return)))
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
                ScoreManager.Instance.AddPendingScore(winnerID, true);
                break;
            }
        }


        if (winnerID >= 0 && winnerID < playerHUDs.Length)
        {
            UIManager.Instance.PlayVictoryAnimation(winnerID);
            yield return null;
            float duration = UIManager.Instance.GetVictoryAnimationDuration();
            yield return new WaitForSeconds(duration);
            playerHUDs[winnerID].AddWin();
            UIManager.Instance.PlayVictoryAnimation(-1);
            yield return new WaitForSeconds(0.75f);
        }
        EndGame();
    }
}
