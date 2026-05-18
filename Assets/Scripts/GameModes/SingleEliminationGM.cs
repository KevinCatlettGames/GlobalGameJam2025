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
        if (gameModeType == GameModeType.Team)
        {
            if (CountAliveTeams() <= 1)
            {
                gameEnded = true;
                CallGameEndClientRpc();
            }
        }
        else
        {
            if (CountAlivePlayers() <= 1)
            {
                gameEnded = true;
                CallGameEndClientRpc();
            }
        }
    }

    public override void CheckForRoundEndLocal()
    {
        if (gameEnded) return;
        if (gameModeType == GameModeType.Team)
        {
            if (CountAliveTeams() <= 1)
            {
                gameEnded = true;
                CallGameEndLocal();
            }
        }
        else
        {
            if (CountAlivePlayers() <= 1)
            {
                gameEnded = true;
                CallGameEndLocal();
            }
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

    private int CountAliveTeams()
    {
        int count = 0;
        foreach (var player in teamA)
        {
            int ID = player.PlayerID;
            PlayerState state = playerStates[ID];
            if (state == PlayerState.alive || state == PlayerState.pendingRespawn)
            {
                count++;
                break;
            }
        }
        foreach (var player in teamB)
        {
            int ID = player.PlayerID;
            PlayerState state = playerStates[ID];
            if (state == PlayerState.alive || state == PlayerState.pendingRespawn)
            {
                count++;
                break;
            }
        }
        return count;
    }

    [ClientRpc]
    private void CallGameEndClientRpc()
    {
        StartCoroutine(AwardVictory());
    }

    private void CallGameEndLocal()
    {
        StartCoroutine(AwardVictory());
    }

    private IEnumerator AwardVictory()
    {
        float danceTime = 1.5f;
        yield return new WaitForSeconds(gameEndDelay);
        int winnerID = -1;
        if (gameModeType == GameModeType.Standard)
        {
            for (int i = 0; i < playerStates.Length; i++)
            {
                if (playerStates[i] == PlayerState.alive)
                {
                    winnerID = i;
                    players[winnerID].Victory();

                    if(gameModeType == GameModeType.Standard)
                        ScoreManager.Instance.AddPendingScore(winnerID, true);
                    else if(gameModeType == GameModeType.Team)
                        ScoreManager.Instance.AddPendingTeamScore(teamIDs[winnerID], true);

                    UnlockRoundEndWithZeroDamageAchievement(winnerID);
                    UnlockRoundEndWithXDamageAchievement(winnerID);
                    break;
                }
            }

            if (winnerID >= 0 && winnerID < playerHUDs.Length)
            {
                yield return new WaitForSeconds(danceTime);
            }
        }
        else if(gameModeType == GameModeType.Team)
        {
            for (int i = 0; i < playerStates.Length; i++)
            {
                if (playerStates[i] == PlayerState.alive)
                {
                    winnerID = teamIDs[i];
                    players[i].Victory();
                    //UnlockRoundEndWithZeroDamageAchievement(winnerID);
                    //UnlockRoundEndWithXDamageAchievement(winnerID);
                }
            }
            if (winnerID != -1)
            {
                if (gameModeType == GameModeType.Standard)
                    ScoreManager.Instance.AddPendingScore(winnerID, true);
                else if (gameModeType == GameModeType.Team)
                    ScoreManager.Instance.AddPendingTeamScore(winnerID, true);
                yield return new WaitForSeconds(danceTime);
            }
        }
        EndGame();
    }
}
