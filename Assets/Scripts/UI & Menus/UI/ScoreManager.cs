using System.Collections;
using System.Collections.Generic;
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
        panel.SetPortrait(portrait, color);
    }

    public void InitialiseTeamScorePanel(int teamID, int playerID)
    {
        if (!activePlayers.Contains(playerID))
            activePlayers.Add(playerID);

        ScorePanel panel = teamModeScorePanels[teamID];
        panel.gameObject.SetActive(true);
        panel.SetPortrait(
            null,
            LobbyManager.instance.TeamColors[teamID]
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
                // Find a player belonging to this team for portrait display
                int representativePlayer = -1;

                foreach (int player in activePlayers)
                {
                    foreach(PlayerController controller in GameManager.Instance.TeamA)
                    {
                        if(player == controller.PlayerID)
                        {
                            representativePlayer = player;
                            break;
                        }
                    }
                    if (representativePlayer != -1)
                        break;
                }

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

    public void ResolveScores()
    {
        StartCoroutine(ResolveScoresCoroutine());
    }

    private IEnumerator ResolveScoresCoroutine()
    {
        if (activePlayers.Count == 0 || activePlayers.Count > 4)
            yield break;

        restartText.SetActive(false);

        // Hide all panels first
        foreach (var panel in standardModeScorePanels)
            panel.gameObject.SetActive(false);

        foreach (var panel in teamModeScorePanels)
            panel.gameObject.SetActive(false);

        yield return new WaitForSeconds(0.2f);

        // STANDARD MODE
        if (GameManager.Instance.GameMode == GameManager.GameModeType.Standard)
        {
            var sorted = GetSortedScores();

            for (int i = 0; i < sorted.Count; i++)
            {
                var entry = sorted[i];

                ScorePanel panel = standardModeScorePanels[i];
                panel.gameObject.SetActive(true);

                panel.SetPortrait(
                    playerHUDs[entry.playerID].Skin.GameSprites[0],
                    playerHUDs[entry.playerID].Skin.Color
                );

                int wins =
                    entry.wins - pendingWins[entry.playerID];

                int kills =
                    entry.kills - pendingKills[entry.playerID];

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

        else if (GameManager.Instance.GameMode ==
                 GameManager.GameModeType.Team)
        {
            for (int teamID = 1;
                 teamID <= 2;
                 teamID++)
            {
                int teamIndex = teamID - 1;

                ScorePanel panel =
                    teamModeScorePanels[teamIndex];

                panel.gameObject.SetActive(true);

                panel.SetPortrait(
                    null,
                    LobbyManager.instance
                    .TeamColors[teamIndex]
                );

                int wins =
                    scores.WinScores[teamIndex]
                    - pendingWins[teamIndex];

                List<PlayerController> teamPlayers =
                    teamID == 1
                    ? GameManager.Instance.TeamA
                    : GameManager.Instance.TeamB;

                int[] teamKills =
                    new int[teamPlayers.Count];

                for (int i = 0;
                     i < teamPlayers.Count;
                     i++)
                {
                    int playerID =
                        teamPlayers[i].PlayerID;

                    teamKills[i] =
                        scores.KillScores[playerID]
                        - pendingKills[playerID];
                }

                panel.SetTeamScores(
                    wins,
                    teamKills
                );
            }

            yield return new WaitForSeconds(0.2f);

            for (int teamID = 1;
                 teamID <= 2;
                 teamID++)
            {
                int teamIndex =
                    teamID - 1;

                ScorePanel panel =
                    teamModeScorePanels[teamIndex];

                for (int w = 0;
                     w < pendingWins[teamIndex];
                     w++)
                {
                    panel.AddWin();
                    yield return
                        new WaitForSeconds(0.1f);
                }
            }
        }

        yield return new WaitForSeconds(2f);

        // Winner check
        if (!GameManager.Instance.playEndless &&
            LobbyManager.instance)
        {
            if (GameManager.Instance.GameMode ==
                GameManager.GameModeType.Team)
            {
                for (int teamID = 0; teamID < 2; teamID++)
                {
                    if (scores.WinScores[teamID] >=
                        LobbyManager.instance.winsNeeded)
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
                    if (scores.WinScores[p] >=
                        LobbyManager.instance.winsNeeded)
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