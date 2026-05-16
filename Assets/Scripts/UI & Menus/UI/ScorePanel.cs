using Febucci.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Color = UnityEngine.Color;

public class ScorePanel : MonoBehaviour
{
    [Header("Score")]
    [SerializeField] private TypewriterByWord winsTypewriter;
    [SerializeField] private TypewriterByWord killsTypewriter;

    [SerializeField] private TextMeshProUGUI winsText;
    [SerializeField] private TextMeshProUGUI[] killsTexts;

    [SerializeField] private Image[] killsIcons;
    [SerializeField] private Image[] portraits;
    [SerializeField] private Image[] pointBubbles;
    [SerializeField] private Image frame;
    [SerializeField] private Color colorShift;

    [SerializeField] private int playerID = -1;

    private int portraitsSet = 0;
    private bool initialSet = false;
    private int kills = 0;
    private int wins = 0;

    private void OnEnable()
    {
        if (initialSet)
            return;

        initialSet = true;

        foreach (Image image in portraits)
            image.enabled = false;

        if (GameManager.Instance.GameMode ==
            GameManager.GameModeType.Team)
        {
            if (playerID == 1)
            {
                for (int i = 0;
                     i < GameManager.Instance.TeamB.Count;
                     i++)
                {
                    portraits[i].enabled = true;
                }
            }
            else if (playerID == 2)
            {
                for (int i = 0;
                     i < GameManager.Instance.TeamA.Count;
                     i++)
                {
                    portraits[i].enabled = true;
                }
            }

            foreach (Image image in killsIcons)
                image.enabled = false;

            foreach (TextMeshProUGUI text in killsTexts)
                text.enabled = false;
        }
    }

    public void SetPortrait(
        Sprite playerPortrait,
        Color playerColor)
    {
        if (GameManager.Instance.GameMode ==
            GameManager.GameModeType.Team)
            return;

        foreach (Image image in portraits)
        {
            image.enabled = true;
            image.sprite = playerPortrait;
        }

        foreach (Image image in pointBubbles)
        {
            image.color = playerColor;
            image.enabled = false;
        }

        frame.color = playerColor - colorShift;
    }

    public void SetTeamPortraits(
        Sprite playerPortrait)
    {
        if (portraitsSet >= portraits.Length)
            return;

        portraits[portraitsSet].sprite =
            playerPortrait;

        portraits[portraitsSet].enabled =
            true;

        portraitsSet++;
    }

    public void AddWin()
    {
        wins++;

        winsTypewriter.ShowText(
            wins.ToString());

        winsTypewriter.SkipTypewriter();

        int visibleWins =
            GetVisibleWins(
                wins,
                pointBubbles.Length
            );

        UpdateBubbles(
            visibleWins,
            playEffect: true
        );
    }

    public void AddKill()
    {
        kills++;

        killsTypewriter.ShowText(
            kills.ToString());

        killsTypewriter.SkipTypewriter();
    }

    public void SetScores(
        int _wins,
        int _kills)
    {
        wins = _wins;
        kills = _kills;

        winsText.text =
            wins.ToString();

        if (killsTexts.Length > 0)
        {
            killsTexts[0].text =
                kills.ToString();

            killsTexts[0].enabled = true;
        }

        if (killsIcons.Length > 0)
            killsIcons[0].enabled = true;

        int visibleWins =
            GetVisibleWins(
                wins,
                pointBubbles.Length
            );

        UpdateBubbles(
            visibleWins,
            playEffect: false
        );
    }

    public void SetTeamScores(
     int _wins,
     int[] playerKills)
    {
        wins = _wins;

        winsText.text =
            wins.ToString();

        for (int i = 0;
             i < killsTexts.Length;
             i++)
        {
            bool playerExists =
                i < playerKills.Length;

            killsTexts[i].enabled =
                playerExists;

            if (i < killsIcons.Length)
            {
                killsIcons[i].enabled =
                    playerExists;
            }

            if (playerExists)
            {
                killsTexts[i].text =
                    playerKills[i]
                    .ToString();
            }
        }

        int visibleWins =
            GetVisibleWins(
                wins,
                pointBubbles.Length
            );

        UpdateBubbles(
            visibleWins,
            playEffect: false
        );
    }

    private int GetVisibleWins(
        int wins,
        int bubbleCount)
    {
        if (wins <= 0)
            return 0;

        int result =
            wins % bubbleCount;

        return result == 0
            ? bubbleCount
            : result;
    }

    private void UpdateBubbles(
        int visibleWins,
        bool playEffect)
    {
        for (int i = 0;
             i < pointBubbles.Length;
             i++)
        {
            var bubble =
                pointBubbles[i];

            var effect =
                bubble.GetComponent
                <ResizeEffect>();

            bubble.enabled =
                i < visibleWins;

            if (effect != null)
            {
                effect.HasPerformedEffect =
                    !playEffect;

                if (playEffect &&
                    i == visibleWins - 1)
                {
                    effect.HasPerformedEffect =
                        false;

                    effect.PlayEffect();
                }
            }
        }
    }
}