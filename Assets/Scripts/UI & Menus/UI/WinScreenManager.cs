using UnityEngine;
using System.Collections.Generic;
using FMODUnity;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WinScreenManager : MonoBehaviour
{
    public static WinScreenManager Instance;

    [SerializeField] private GameObject gameUI;
    [SerializeField] private SO_Scores scores;
    [SerializeField] private GameObject[] winPanels;
    [SerializeField] private Outline[] outlines;
    [SerializeField] private Image[] killImages;
    [SerializeField] private TextMeshProUGUI[] killCounts;
    [SerializeField] private Image teamImage;
    [SerializeField] private TextMeshProUGUI teamKillText;
    [SerializeField] private Image[] playerImages;
    [SerializeField] private Image[] nonWinnerImages;
    [SerializeField] private Image[] nonWinnerBadgeImages;
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
        foreach (var panel in winPanels)
            panel.SetActive(false);

        List<int> winnerPlayerIDs =
            GameManager.Instance.GameMode ==
            GameManager.GameModeType.Standard
            ? GetStandardWinners()
            : GetTeamWinners();

        int winnerCount = winnerPlayerIDs.Count;

        for (int i = 0; i < winnerCount; i++)
        {
            int playerID = winnerPlayerIDs[i];
            winPanels[i].SetActive(true);

            outlines[i].effectColor =
                LobbyPlayerValues.Instance
                .playerValuesList[playerID]
                .Skin.Color;

            //RectTransform rectTransform =
            //    winPanels[i].GetComponent<RectTransform>();

            //float xPosition =
            //    (i - (winnerCount - 1) / 2f)
            //    * panelSpacing;

            //rectTransform.anchoredPosition =
            //    new Vector2(
            //        xPosition,
            //        rectTransform.anchoredPosition.y
            //    );

            playerImages[i].sprite =
                LobbyPlayerValues.Instance
                .playerValuesList[playerID]
                .Skin.SplashArt;

            if (GameManager.Instance.GameMode == GameManager.GameModeType.Standard)
            {
                killCounts[i].text =
                    scores.KillScores[playerID]
                    .ToString();
            }
            else
            {
                killCounts[i].enabled = false;
                killImages[i].enabled = false;
                if (scores.KillScores[playerID] == 0) return;
                teamImage.enabled = true;
                teamKillText.enabled = true;
                teamKillText.text = scores.KillScores[playerID].ToString();
            }

        }

        List<ScoreManager.PlayerScoreEntry> playerScoreEntries = ScoreManager.Instance.GetScores(false);
        int imageIndex = 0;
        for (int i = winnerCount; i < playerScoreEntries.Count; i++)
        {
            SkinSO skin = LobbyPlayerValues.Instance.playerValuesList[playerScoreEntries[i].playerID].Skin;
            nonWinnerImages[imageIndex].enabled = true;
            nonWinnerImages[imageIndex].sprite = skin.HeadSprites[0];
            nonWinnerBadgeImages[imageIndex].enabled = true;
            nonWinnerBadgeImages[imageIndex].color = skin.Color;
            imageIndex++;
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