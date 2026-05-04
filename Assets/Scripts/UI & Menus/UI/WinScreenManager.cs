using UnityEngine;
using System.Collections.Generic;
using FMODUnity;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class WinScreenManager : MonoBehaviour
{
    public static WinScreenManager Instance;

    [SerializeField] private SO_Scores scores;
    [SerializeField] private GameObject[] winPanels;
    [SerializeField] private TextMeshProUGUI[] killCounts;
    [SerializeField] private Image[] playerImages;
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
        eventSystem.SetSelectedGameObject(restartButton.gameObject);
        ShowWinnerUsingWinScore();
    }

    public void ShowWinnerUsingWinScore()
    {
        int highestScore = -1;
        List<int> winnerPlayerIDs = new();

        for (int i = 0; i < scores.WinScores.Length; i++)
        {
            int score = scores.WinScores[i];

            if (score > highestScore)
            {
                highestScore = score;
                winnerPlayerIDs.Clear();
                AddPlayerOrTeamToWinners(i, winnerPlayerIDs);
            }
            else if (score == highestScore)
            {
                AddPlayerOrTeamToWinners(i, winnerPlayerIDs);
            }
        }

        foreach (var panel in winPanels)
            panel.SetActive(false);
        
        int winnerCount = winnerPlayerIDs.Count;
        for (int i = 0; i < winnerCount; i++)
        {
            winPanels[i].SetActive(true);

            RectTransform rectTransform = winPanels[i].GetComponent<RectTransform>();
            float xPosition = (i - (winnerCount - 1) / 2f) * panelSpacing;
            rectTransform.anchoredPosition = new Vector2(xPosition, rectTransform.anchoredPosition.y);

            int playerID = winnerPlayerIDs[i];

            playerImages[i].sprite = LobbyPlayerValues.Instance.playerValuesList[playerID].Skin.LobbySprite;
            killCounts[i].text = scores.KillScores[playerID].ToString();
        }

        emitter.Play();
    }
    
    private void AddPlayerOrTeamToWinners(int winScoreIndex, List<int> winnerPlayerIDs)
    {
        if (GameManager.Instance.GameMode == GameManager.GameModeType.Team)
        {
            // Step 1: Find which team has the player whose WinScore is at winScoreIndex
            for (int t = 0; t < 2; t++)
            {
                List<PlayerController> teamPlayers = GameManager.Instance.GetTeam(t);

                // Look for any player in this team whose WinScore matches the one at winScoreIndex
                bool teamHasWinner = false;
                foreach (var player in teamPlayers)
                {
                    if (scores.WinScores[player.PlayerID] == scores.WinScores[winScoreIndex])
                    {
                        teamHasWinner = true;
                        break;
                    }
                }

                if (teamHasWinner)
                {
                    // Add all players from this team to winnerPlayerIDs
                    foreach (var player in teamPlayers)
                    {
                        if (!winnerPlayerIDs.Contains(player.PlayerID))
                            winnerPlayerIDs.Add(player.PlayerID);
                    }
                    break; // team found, stop looking further
                }
            }
        }
        else
        {
            // Free-for-all: just the player at winScoreIndex
            if (!winnerPlayerIDs.Contains(winScoreIndex))
                winnerPlayerIDs.Add(winScoreIndex);
        }
    }
}