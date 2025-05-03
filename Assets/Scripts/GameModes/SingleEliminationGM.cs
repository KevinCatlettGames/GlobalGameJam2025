using System.Collections;
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
            StartCoroutine(AwardVictory());
        }
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
        EndGame();
    }

}
