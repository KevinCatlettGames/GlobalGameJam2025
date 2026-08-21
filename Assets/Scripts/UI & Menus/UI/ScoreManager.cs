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

    [Header("Animation Settings")]
    [SerializeField] private float reorderDuration = 0.6f;

    private int[] pendingWins = new int[4];
    private int[] pendingKills = new int[4];

    private List<int> activePlayers = new List<int>();

    private bool scoresResolved = false;
    public bool ScoresResolved => scoresResolved;

    public bool showWinner = false;

    // Cache layout slot anchor positions for baseline vertical heights
    private Vector2[] standardSlotPositions;
    private Vector2[] teamSlotPositions;

    [SerializeField] private GameObject winnerShine;

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

        CacheSlotPositions();
    }

    private void CacheSlotPositions()
    {
        if (standardModeScorePanels != null && standardModeScorePanels.Length > 0)
        {
            standardSlotPositions = new Vector2[standardModeScorePanels.Length];
            for (int i = 0; i < standardModeScorePanels.Length; i++)
            {
                RectTransform rt = standardModeScorePanels[i].GetComponent<RectTransform>();
                standardSlotPositions[i] = rt.anchoredPosition;
            }
        }

        if (teamModeScorePanels != null && teamModeScorePanels.Length > 0)
        {
            teamSlotPositions = new Vector2[teamModeScorePanels.Length];
            for (int i = 0; i < teamModeScorePanels.Length; i++)
            {
                RectTransform rt = teamModeScorePanels[i].GetComponent<RectTransform>();
                teamSlotPositions[i] = rt.anchoredPosition;
            }
        }
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

    public struct PlayerScoreEntry
    {
        public int playerID;
        public int displayPlayerID;
        public int teamID;
        public int wins;
        public int kills;
    }

    public struct TeamScoreEntry
    {
        public int teamID;
        public List<PlayerController> teamPlayers;
        public int wins;
        public int kills;
    }

    public List<PlayerScoreEntry> GetScores(bool usePreviousScores)
    {
        List<PlayerScoreEntry> list = new List<PlayerScoreEntry>();

        if (GameManager.Instance.GameMode == GameManager.GameModeType.Standard)
        {
            foreach (int id in activePlayers)
            {
                int wins = scores.WinScores[id] - (usePreviousScores ? pendingWins[id] : 0);
                int kills = scores.KillScores[id] - (usePreviousScores ? pendingKills[id] : 0);

                list.Add(new PlayerScoreEntry
                {
                    playerID = id,
                    displayPlayerID = id,
                    teamID = -1,
                    wins = wins,
                    kills = kills
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

                int wins = scores.WinScores[team] - (usePreviousScores ? pendingWins[team] : 0);
                int kills = scores.KillScores[team] - (usePreviousScores ? pendingKills[team] : 0);

                list.Add(new PlayerScoreEntry
                {
                    playerID = -1,
                    displayPlayerID = representativePlayer,
                    teamID = team,
                    wins = wins,
                    kills = kills
                });
            }
        }

        list.Sort((a, b) =>
        {
            int result = b.wins.CompareTo(a.wins);
            if (result != 0) return result;
            return b.kills.CompareTo(a.kills);
        });

        return list;
    }

    public List<TeamScoreEntry> GetTeamScores(bool usePreviousScores)
    {
        List<TeamScoreEntry> list = new List<TeamScoreEntry>();

        for (int team = 0; team < 2; team++)
        {
            List<PlayerController> teamPlayers =
                team == 0
                ? GameManager.Instance.TeamA
                : GameManager.Instance.TeamB;

            // Retrieve team total kills directly from scores.KillScores[team]
            int teamTotalKills = scores.KillScores[team];

            int wins = scores.WinScores[team] - (usePreviousScores ? pendingWins[team] : 0);
            int kills = teamTotalKills - (usePreviousScores ? pendingKills[team] : 0);

            list.Add(new TeamScoreEntry
            {
                teamID = team,
                teamPlayers = teamPlayers,
                wins = wins,
                kills = kills
            });
        }

        list.Sort((a, b) =>
        {
            int result = b.wins.CompareTo(a.wins);
            if (result != 0) return result;
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
        winnerShine.SetActive(false);
        foreach (var panel in standardModeScorePanels)
        {
            panel.gameObject.SetActive(false);
        }

        yield return new WaitForSeconds(0.2f);

        if (GameManager.Instance.GameMode == GameManager.GameModeType.Standard)
        {
            var previousSorted = GetScores(usePreviousScores: true);

            for (int i = 0; i < previousSorted.Count; i++)
            {
                var entry = previousSorted[i];
                ScorePanel panel = standardModeScorePanels[entry.playerID];
                panel.gameObject.SetActive(true);

                RectTransform rt = panel.GetComponent<RectTransform>();
                rt.anchoredPosition = standardSlotPositions[i];

                panel.SetPortrait(
                    playerHUDs[entry.playerID].Skin.HeadSprites[0],
                    playerHUDs[entry.playerID].Skin.Color,
                    0
                );

                panel.SetScores(entry.wins, entry.kills);
            }

            yield return new WaitForSeconds(0.2f);

            for (int i = 0; i < previousSorted.Count; i++)
            {
                var entry = previousSorted[i];
                ScorePanel panel = standardModeScorePanels[entry.playerID];

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

            yield return new WaitForSeconds(0.3f);

            //var newSorted = GetScores(usePreviousScores: false);

            //foreach (var entry in newSorted)
            //{
            //    standardModeScorePanels[entry.playerID].SetScores(entry.wins, entry.kills);
            //}
            var newSorted = GetScores(usePreviousScores: false);
            yield return StartCoroutine(AnimateStandardPanelsReorder(newSorted));

            // CHECK FOR NEW LEADER (STANDARD)
            int oldLeaderID = previousSorted[0].playerID;
            int newLeaderID = newSorted[0].playerID;

            if (oldLeaderID != newLeaderID)
            {
                winnerShine.SetActive(true);
            }
        }
        else if (GameManager.Instance.GameMode == GameManager.GameModeType.Team)
        {
            var previousSortedTeams = GetTeamScores(usePreviousScores: true);

            for (int i = 0; i < previousSortedTeams.Count; i++)
            {
                var entry = previousSortedTeams[i];
                ScorePanel panel = teamModeScorePanels[entry.teamID];
                panel.gameObject.SetActive(true);

                RectTransform rt = panel.GetComponent<RectTransform>();
                rt.anchoredPosition = teamSlotPositions[i];

                List<PlayerController> teamPlayers = entry.teamID == 0 ? GameManager.Instance.TeamA : GameManager.Instance.TeamB;

                for (int j = 0; j < teamPlayers.Count; j++)
                {
                    Sprite sprite = teamPlayers.Count > 0 ? teamPlayers[j].CurrentSkinSO.HeadSprites[0] : null;
                    panel.SetPortrait(sprite, LobbyManager.instance.TeamColors[entry.teamID], j);
                }

                panel.SetTeamScores(entry.wins, entry.kills);
            }

            yield return new WaitForSeconds(0.2f);

            // Add pending team wins and kills visually
            for (int i = 0; i < previousSortedTeams.Count; i++)
            {
                var entry = previousSortedTeams[i];
                ScorePanel panel = teamModeScorePanels[entry.teamID];

                for (int w = 0; w < pendingWins[entry.teamID]; w++)
                {
                    panel.AddWin();
                    yield return new WaitForSeconds(0.1f);
                }

                for (int k = 0; k < pendingKills[entry.teamID]; k++)
                {
                    panel.AddKill();
                    yield return new WaitForSeconds(0.1f);
                }
            }

            yield return new WaitForSeconds(0.3f);

            var newSortedTeams = GetTeamScores(usePreviousScores: false);
            yield return StartCoroutine(AnimateTeamPanelsReorder(newSortedTeams));
        }

        yield return new WaitForSeconds(1.5f);

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

            if (NetworkManager.Singleton.IsServer)
                restartText.SetActive(true);
        }

        scoresResolved = true;
    }

    private IEnumerator AnimateStandardPanelsReorder(List<PlayerScoreEntry> newSorted)
    {
        float elapsed = 0f;
        Vector2[] startPositions = new Vector2[standardModeScorePanels.Length];
        Vector2[] targetPositions = new Vector2[standardModeScorePanels.Length];

        for (int i = 0; i < newSorted.Count; i++)
        {
            int playerID = newSorted[i].playerID;
            RectTransform rt = standardModeScorePanels[playerID].GetComponent<RectTransform>();
            startPositions[playerID] = rt.anchoredPosition;
            targetPositions[playerID] = standardSlotPositions[i];
        }

        while (elapsed < reorderDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / reorderDuration);

            foreach (var entry in newSorted)
            {
                int pID = entry.playerID;
                RectTransform rt = standardModeScorePanels[pID].GetComponent<RectTransform>();
                rt.anchoredPosition = Vector2.Lerp(startPositions[pID], targetPositions[pID], t);
            }

            yield return null;
        }

        foreach (var entry in newSorted)
        {
            int pID = entry.playerID;
            standardModeScorePanels[pID].GetComponent<RectTransform>().anchoredPosition = targetPositions[pID];
        }
    }

    private IEnumerator AnimateTeamPanelsReorder(List<TeamScoreEntry> newSortedTeams)
    {
        float elapsed = 0f;
        Vector2[] startPositions = new Vector2[teamModeScorePanels.Length];
        Vector2[] targetPositions = new Vector2[teamModeScorePanels.Length];

        for (int i = 0; i < newSortedTeams.Count; i++)
        {
            int teamID = newSortedTeams[i].teamID;
            RectTransform rt = teamModeScorePanels[teamID].GetComponent<RectTransform>();
            startPositions[teamID] = rt.anchoredPosition;
            targetPositions[teamID] = teamSlotPositions[i];
        }

        while (elapsed < reorderDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / reorderDuration);

            foreach (var entry in newSortedTeams)
            {
                int tID = entry.teamID;
                RectTransform rt = teamModeScorePanels[tID].GetComponent<RectTransform>();
                rt.anchoredPosition = Vector2.Lerp(startPositions[tID], targetPositions[tID], t);
            }

            yield return null;
        }

        foreach (var entry in newSortedTeams)
        {
            int tID = entry.teamID;
            teamModeScorePanels[tID].GetComponent<RectTransform>().anchoredPosition = targetPositions[tID];
        }
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