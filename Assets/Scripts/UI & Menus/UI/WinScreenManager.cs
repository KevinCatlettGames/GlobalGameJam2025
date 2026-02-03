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
    [SerializeField] private TextMeshProUGUI[] winCounts;
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
        List<int> winners = new();

        for (int i = 0; i < scores.WinScores.Length; i++)
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

        for (int i = 0; i < winPanels.Length; i++)
        {
            winPanels[i].SetActive(false);
        }

        int winnerCount = winners.Count;

        for (int i = 0; i < winnerCount; i++)
        {
            winPanels[i].SetActive(true);

            RectTransform rectTransform = winPanels[i].GetComponent<RectTransform>();
            float xPosition = (i - (winnerCount - 1) / 2f) * panelSpacing;
            rectTransform.anchoredPosition = new Vector2(
                xPosition,
                rectTransform.anchoredPosition.y
            );

            int playerIndex = winners[i];

            playerImages[i].sprite =
                LobbyPlayerHandler.Instance.playerValuesList[playerIndex].Skin.LobbySprite;

            winCounts[i].text = scores.WinScores[playerIndex].ToString();
            killCounts[i].text = scores.KillScores[playerIndex].ToString();
        }

        emitter.Play();
    }
}