using FMODUnity;
using System.Collections.Generic;
using TMPro;
using UnityEditor.Localization.Plugins.XLIFF.V12;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WinScreenManager : MonoBehaviour
{
    public static WinScreenManager Instance;

    [SerializeField] private GameObject gameUI;
    [SerializeField] private SO_Scores scores;
    [SerializeField] private Image[] nonWinnerBadgeImages;
    [SerializeField] private WinPanel[] winnerPanels;
    [SerializeField] private WinPanel[] loserPanels;
    [SerializeField] private StudioEventEmitter emitter;
    [SerializeField] private EventSystem eventSystem;
    [SerializeField] private Button restartButton;
    [SerializeField] private float panelSpacing = 400f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            return;
        }

        Destroy(gameObject);
    }

    private void OnEnable()
    {
        gameUI.SetActive(false);
        eventSystem.SetSelectedGameObject(restartButton.gameObject);
        ShowWinnerUsingWinScore();
    }

    public void ShowWinnerUsingWinScore()
    {
        List<int> winnerPlayerIDs =
            GameManager.Instance.GameMode ==
            GameManager.GameModeType.Standard
            ? GetStandardWinners()
            : GetTeamWinners();

        int winnerCount = winnerPlayerIDs.Count;

        float xPositionStart = winnerPanels[0].GetComponent<RectTransform>().anchoredPosition.x;

        for (int i = 0; i < winnerCount; i++)
        {
            int playerID = winnerPlayerIDs[i];

            winnerPanels[i].SetPanel(
                LobbyPlayerValues.Instance.playerValuesList[playerID].Skin.SplashArt,
                scores.WinScores[playerID],
                scores.KillScores[playerID]);

            RectTransform rectTransform =
                winnerPanels[i].GetComponent<RectTransform>();

            float xPosition =
                (i - (winnerCount - 1) / 2f)
                * panelSpacing;
            xPosition += xPositionStart;
            rectTransform.anchoredPosition =
                new Vector2(
                    xPosition,
                    rectTransform.anchoredPosition.y
                );

            if (GameManager.Instance.GameMode == GameManager.GameModeType.Standard)
            {
                List<ScoreManager.PlayerScoreEntry> playerScoreEntries = ScoreManager.Instance.GetScores(false);
                int imageIndex = 0;
                for (int x = winnerCount; x < playerScoreEntries.Count; x++)
                {
                    int loserID = playerScoreEntries[x].playerID;
                    SkinSO skin = LobbyPlayerValues.Instance.playerValuesList[loserID].Skin;
                    nonWinnerBadgeImages[imageIndex].enabled = true;
                    nonWinnerBadgeImages[imageIndex].color = skin.Color;
                    loserPanels[imageIndex].SetPanel(skin.HeadSprites[0], scores.WinScores[loserID], scores.KillScores[loserID]);
                    imageIndex++;
                }
            }
            else
            {
                List<ScoreManager.TeamScoreEntry> teamScoreEntries = ScoreManager.Instance.GetTeamScores(false);
                List<PlayerController> loserTeam = teamScoreEntries[1].teamPlayers;
                int imageIndex = 0;
                for (int y = 0; y < loserTeam.Count; y++)
                {
                    SkinSO skin = loserTeam[y].CurrentSkinSO;
                    nonWinnerBadgeImages[imageIndex].enabled = true;
                    nonWinnerBadgeImages[imageIndex].color = skin.Color;
                    nonWinnerBadgeImages[imageIndex].enabled = true;
                    nonWinnerBadgeImages[imageIndex].color = skin.Color;
                    loserPanels[imageIndex].SetPanel(skin.HeadSprites[0], scores.WinScores[loserTeam[y].PlayerID], scores.KillScores[loserTeam[y].PlayerID]);
                    imageIndex++;
                }
            }

        }

        emitter.Play();
    }

    private List<int> GetStandardWinners()
    {
        List<int> winners = new();

        int highestScore = -1;

        for (int i = 0;
             i < scores.WinScores.Length;
             i++)
        {
            int score = scores.WinScores[i];

            if (score > highestScore)
            {
                highestScore = score;
                winners.Clear();
                winners.Add(i);
            }
            else if (score == highestScore)
            {
                winners.Add(i);
            }
        }

        return winners;
    }

    private List<int> GetTeamWinners()
    {
        List<int> winners = new();

        int highestScore = -1;
        List<int> winningTeams = new();

        for (int team = 0; team < 2; team++)
        {
            int score = scores.WinScores[team];

            if (score > highestScore)
            {
                highestScore = score;
                winningTeams.Clear();
                winningTeams.Add(team);
            }
            else if (score == highestScore)
            {
                winningTeams.Add(team);
            }
        }

        foreach (int teamID in winningTeams)
        {
            List<PlayerController> teamPlayers =
                teamID == 0
                ? GameManager.Instance.TeamA
                : GameManager.Instance.TeamB;

            foreach (var player in teamPlayers)
            {
                winners.Add(player.PlayerID);
            }
        }

        return winners;
    }

}