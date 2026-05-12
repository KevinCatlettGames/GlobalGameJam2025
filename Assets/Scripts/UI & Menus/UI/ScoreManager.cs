using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [SerializeField] private ScorePanel[] scorePanels;
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
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void InitialiseScorePanel(int playerID, Sprite portrait, Color color)
    {
        if (!activePlayers.Contains(playerID))
            activePlayers.Add(playerID);

        ScorePanel panel = scorePanels[playerID];
        panel.gameObject.SetActive(true);
        panel.SetPortrait(portrait, color);
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

    private struct PlayerScoreEntry
    {
        public int playerID;
        public int wins;
        public int kills;
    }

    private List<PlayerScoreEntry> GetSortedScores()
    {
        List<PlayerScoreEntry> list = new List<PlayerScoreEntry>();

        foreach (int id in activePlayers)
        {
            list.Add(new PlayerScoreEntry
            {
                playerID = id,
                wins = scores.WinScores[id],
                kills = scores.KillScores[id]
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

        for (int i = 0; i < scorePanels.Length; i++)
            scorePanels[i].gameObject.SetActive(false);

        var sorted = GetSortedScores();

        yield return new WaitForSeconds(0.2f);

        for (int i = 0; i < sorted.Count; i++)
        {
            var entry = sorted[i];

            ScorePanel panel = scorePanels[i];
            panel.gameObject.SetActive(true);

            panel.SetPortrait(
                playerHUDs[entry.playerID].Skin.GameSprites[0],
                playerHUDs[entry.playerID].Skin.Color
            );

            int wins = entry.wins - pendingWins[entry.playerID];
            int kills = entry.kills - pendingKills[entry.playerID];

            panel.SetScores(wins, kills);

            yield return new WaitForSeconds(0.1f);

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

            yield return new WaitForSeconds(0.2f);
        }

        yield return new WaitForSeconds(2f);

        if (!GameManager.Instance.playEndless && LobbyManager.instance)
        {
            foreach (var p in activePlayers)
            {
                if (scores.WinScores[p] >= LobbyManager.instance.winsNeededToWin)
                {
                    showWinner = true;
                    break;
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