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
        if (CountAlivePlayers() <= 1)
        {
            CallGameEndClientRpc();
        }
    }

    public override void CheckForRoundEndLocal()
    {
        if (CountAlivePlayers() <= 1)
        {
            CallGameEndLocal();
        }
    }

    private int CountAlivePlayers()
    {
        int count = 0;
        foreach (var state in playerStates)
        {
            if (state == PlayerState.alive) count++;
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


        if (winnerID >= 0 && winnerID < playerHUDs.Length)
        {
            victoryAnimator.gameObject.SetActive(true);
            victoryAnimator.Play($"P{winnerID}");
            yield return new WaitForSeconds(1f);
            playerHUDs[winnerID].AddWin();
            victoryAnimator.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.75f);
        }
        EndGame();
    }
}
