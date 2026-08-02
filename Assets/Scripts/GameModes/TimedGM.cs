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
            if (gameModeType == GameModeType.Standard)
                ScoreManager.Instance.AddPendingScore(winnerID, true);
            else if (gameModeType == GameModeType.Team)
                ScoreManager.Instance.AddPendingTeamScore(teamIDs[winnerID], true);

        if (winnerID >= 0 && winnerID < playerHUDs.Length)
        {
            yield return new WaitForSeconds(0.75f);
        }
        EndGame();
    }
    private void OnDestroy()
    {
        if (LobbyManager.instance && LobbyManager.instance.SelectedGameMode != gameModeType
            || !LobbyManager.instance && gameModeType != GameModeType.Standard)
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
