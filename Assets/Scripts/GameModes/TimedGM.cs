using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class TimedGM : GameManager
{
    private Timer timer;
    private void Start()
    {
        Time.timeScale = 1;
        timer = UIManager.Instance.GetTimer();
        OnGameStarted += StartTimer;
        if (PlayingLocal)
        {
            timer.OnTimerComplete += CallGameEndLocal;
        }
        else
        {
            timer.OnTimerComplete += CallGameEndClientRpc;
        }
        timer.StartTimer(gameSettings.Time);
    }

    private void StartTimer()
    {
        timer.StartTimer(gameSettings.Time);
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
        int[] kills = ScoreManager.Instance.GetKillScores();
        int currentTopScore = 0;
        for (int i = 0; i < kills.Length; i++)
        {
            if (kills[i] > currentTopScore)
            {
                winnerID = i;
            }
        }
        if (winnerID != -1)
            ScoreManager.Instance.AddPendingScore(winnerID, true);

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
    private void OnDestroy()
    {
        if (LobbyManager.instance && LobbyManager.instance.SelectedGameMode != gameModeType
            || !LobbyManager.instance && gameModeType != GameModeType.SingleElimination)
            return;
            
        OnGameStarted -= StartTimer;
        if (PlayingLocal)
        {
            timer.OnTimerComplete += CallGameEndLocal;
        }
        else
        {
            timer.OnTimerComplete += CallGameEndClientRpc;
        }
    }
}
