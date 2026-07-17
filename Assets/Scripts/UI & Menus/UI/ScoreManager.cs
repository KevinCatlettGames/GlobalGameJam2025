using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [SerializeField] private ScorePanel[] standardModeScorePanels;
    [SerializeField] public ScorePanel[] teamModeScorePanels;
    [SerializeField] private PlayerHUD[] playerHUDs;
    [SerializeField] private SO_Scores scores;

    [SerializeField] private GameObject restartText;
    [SerializeField] private GameObject scoreScreen;
    [SerializeField] private GameObject winScreen;

    private int[] pendingWins = new int[4];
    private int[] pendingKills = new int[4];

    private List<int> activePlayers = new List<int>();

    private bool scoresResolved = false;
    public bool ScoresResolved => scoresResolved;

    public bool showWinner = false;

    public struct TeamKillEntry
    {
        public int playerID;
        public int teamID;
        public int kills;
    }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void InitialiseScorePanel(int playerID, Sprite portrait, Color color)
    {
        if (!activePlayers.Contains(playerID))
            activePlayers.Add(playerID);

        ScorePanel panel = standardModeScorePanels[playerID];
        panel.gameObject.SetActive(true);
        panel.SetPortrait(portrait, color, 0);
    }

    public void InitialiseTeamScorePanel(int teamID, int playerID)
    {
        if (!activePlayers.Contains(playerID))
            activePlayers.Add(playerID);

        ScorePanel panel = teamModeScorePanels[teamID];
        panel.gameObject.SetActive(true);

        panel.SetPortrait(
            null,
            LobbyManager.instance.TeamColors[teamID], 
            0
        );
    }

    public void AddPendingScore(int playerID, bool isWin)
    {
        scoresResolved = false;

        if (isWin)
        {
            pendingWins[playerID]++;
            scores.WinScores[playerID]++;
        }
        else
        {
            pendingKills[playerID]++;
            scores.KillScores[playerID]++;
        }
    }

    public void AddPendingTeamScore(int teamID, bool isWin)
    {
        scoresResolved = false;

        int teamIndex = teamID - 1;

        if (isWin)
        {
            pendingWins[teamIndex]++;
            scores.WinScores[teamIndex]++;
        }
        else
        {
            pendingKills[teamIndex]++;
            scores.KillScores[teamIndex]++;
        }
    }

    private struct PlayerScoreEntry
    {
        public int playerID;
        public int displayPlayerID;
        public int teamID;
        public int wins;
        public int kills;
    }

    private struct TeamScoreEntry
    {
        public int teamID;
        public List<PlayerController> teamPlayers;
        public int wins;
        public int kills;
    }

    private List<PlayerScoreEntry> GetSortedScores()
    {
        List<PlayerScoreEntry> list = new List<PlayerScoreEntry>();

        if (GameManager.Instance.GameMode == GameManager.GameModeType.Standard)
        {
            foreach (int id in activePlayers)
            {
                list.Add(new PlayerScoreEntry
                {
                    playerID = id,
                    displayPlayerID = id,
                    teamID = -1,
                    wins = scores.WinScores[id],
                    kills = scores.KillScores[id]
                });
            }
        }
        else if (GameManager.Instance.GameMode == GameManager.GameModeType.Team)
        {
            for (int team = 0; team < 2; team++)
            {
                int representativePlayer = -1;

                List<PlayerController> teamList =
                    team == 0
                    ? GameManager.Instance.TeamA
                    : GameManager.Instance.TeamB;

                if (teamList.Count > 0)
                    representativePlayer = teamList[0].PlayerID;

                list.Add(new PlayerScoreEntry
                {
                    playerID = -1,
                    displayPlayerID = representativePlayer,
                    teamID = team,
                    wins = scores.WinScores[team],
                    kills = scores.KillScores[team]
                });
            }
        }

        list.Sort((a, b) =>
        {
            int result = b.wins.CompareTo(a.wins);

            if (result != 0)
                return result;

            return b.kills.CompareTo(a.kills);
        });

        return list;
    }

    private List<TeamScoreEntry> GetSortedTeamScores()
    {
        List<TeamScoreEntry> list = new List<TeamScoreEntry>();

        for (int team = 0; team < 2; team++)
        {
            int totalKills = 0;

            List<PlayerController> teamPlayers =
                team == 0
                ? GameManager.Instance.TeamA
                : GameManager.Instance.TeamB;

            foreach (var player in teamPlayers)
            {
                totalKills += scores.KillScores[player.PlayerID];
            }

            list.Add(new TeamScoreEntry
            {
                teamID = team,
                teamPlayers = teamPlayers,
                wins = scores.WinScores[team],
                kills = totalKills
            });
        }

        list.Sort((a, b) =>
        {
            int result = b.wins.CompareTo(a.wins);

            if (result != 0)
                return result;

            return b.kills.CompareTo(a.kills);
        });

        return list;
    }

    public void ResolveScores()
    {
        StartCoroutine(ResolveScoresCoroutine());
    }

    private IEnumerator ResolveScoresCoroutine()
    {
        if (activePlayers.Count == 0 || activePlayers.Count > 4)
            yield break;

        restartText.SetActive(false);

        foreach (var panel in standardModeScorePanels)
            panel.gameObject.SetActive(false);

        foreach (var panel in teamModeScorePanels)
            panel.gameObject.SetActive(false);

        yield return new WaitForSeconds(0.2f);

        if (GameManager.Instance.GameMode == GameManager.GameModeType.Standard)
        {
            var sorted = GetSortedScores();

            for (int i = 0; i < sorted.Count; i++)
            {
                var entry = sorted[i];

                ScorePanel panel = standardModeScorePanels[i];
                panel.gameObject.SetActive(true);

                panel.SetPortrait(
                    playerHUDs[entry.playerID].Skin.HeadSprites[0],
                    playerHUDs[entry.playerID].Skin.Color,
                    0
                );

                int wins = entry.wins - pendingWins[entry.playerID];
                int kills = entry.kills - pendingKills[entry.playerID];

                panel.SetScores(wins, kills);
            }

            yield return new WaitForSeconds(0.2f);

            for (int i = 0; i < sorted.Count; i++)
            {
                var entry = sorted[i];
                ScorePanel panel = standardModeScorePanels[i];

                for (int w = 0; w < pendingWins[entry.playerID]; w++)
                {
                    panel.AddWin();
                    yield return new WaitForSeconds(0.1f);
                }

                for (int k = 0; k < pendingKills[entry.playerID]; k++)
                {
                    panel.AddKill();
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }
        else if (GameManager.Instance.GameMode == GameManager.GameModeType.Team)
        {
            var sortedTeams = GetSortedTeamScores();

            for (int i = 0; i < sortedTeams.Count; i++)
            {
                var entry = sortedTeams[i];

                ScorePanel panel = teamModeScorePanels[i];
                panel.gameObject.SetActive(true);

                List<PlayerController> teamPlayers =
                    entry.teamID == 0
                    ? GameManager.Instance.TeamA
                    : GameManager.Instance.TeamB;

                for(int j = 0; j < teamPlayers.Count; j++)
                {
                    Sprite sprite = teamPlayers.Count > 0 ? teamPlayers[j].CurrentSkinSO.HeadSprites[0] : null;

                    panel.SetPortrait(
                        sprite,
                        LobbyManager.instance.TeamColors[entry.teamID],
                        j
                    );
                }

                int wins = entry.wins - pendingWins[entry.teamID];       
             
                panel.SetTeamScores(wins, scores.KillScores[entry.teamID]);
            }

            yield return new WaitForSeconds(0.2f);

            for (int i = 0; i < sortedTeams.Count; i++)
            {
                var entry = sortedTeams[i];
                ScorePanel panel = teamModeScorePanels[i];

                for (int w = 0; w < pendingWins[entry.teamID]; w++)
                {
                    panel.AddWin();
                    yield return new WaitForSeconds(0.1f);
                }
            }
        }

        yield return new WaitForSeconds(2f);

        if (!GameManager.Instance.playEndless && LobbyManager.instance)
        {
            if (GameManager.Instance.GameMode == GameManager.GameModeType.Team)
            {
                for (int teamID = 0; teamID < 2; teamID++)
                {
                    if (scores.WinScores[teamID] >= LobbyManager.instance.winsNeeded)
                    {
                        showWinner = true;
                        break;
                    }
                }
            }
            else
            {
                foreach (var p in activePlayers)
                {
                    if (scores.WinScores[p] >= LobbyManager.instance.winsNeeded)
                    {
                        showWinner = true;
                        break;
                    }
                }
            }
        }

        if (showWinner)
        {
            restartText.SetActive(false);
            winScreen.SetActive(true);
            scoreScreen.SetActive(false);
        }
        else
        {
            pendingWins = new int[4];
            pendingKills = new int[4];

            if(NetworkManager.Singleton.IsServer)
                restartText.SetActive(true);
        }

        scoresResolved = true;
    }

    public void ResetScores()
    {
        scores.ResetWins();
        scores.ResetKills();
    }

    private void OnApplicationQuit()
    {
        ResetScores();
    }

    public int[] GetKillScores()
    {
        return scores.KillScores;
    }
}