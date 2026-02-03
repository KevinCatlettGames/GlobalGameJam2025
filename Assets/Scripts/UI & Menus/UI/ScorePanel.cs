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
    [SerializeField] private TextMeshProUGUI killsText;
    [SerializeField] private Image portrait;
    [SerializeField] private Image[] pointBubbles;
    [SerializeField] private Image frame;
    [SerializeField] private Color colorShift;

    private int kills = 0;
    private int wins = 0;

    public void SetPortrait(Sprite playerPortrait, Color playerColor)
    {
        portrait.sprite = playerPortrait;

        foreach (Image image in pointBubbles)
        {
            image.color = playerColor;
            image.enabled = false;
        }

        frame.color = playerColor - colorShift;
    }

    public void AddWin()
    {
        wins++;

        winsTypewriter.ShowText(wins.ToString());
        winsTypewriter.SkipTypewriter();

        int bubbleCount = pointBubbles.Length;
        int visibleWins = wins % bubbleCount;

        if (visibleWins == 0 && wins > 0)
            visibleWins = 1;

        for (int i = 0; i < bubbleCount; i++)
        {
            pointBubbles[i].enabled = i < visibleWins;
        }

        var effect = pointBubbles[visibleWins - 1].GetComponent<PointBubbleResizeEffect>();
        effect.HasPerformedEffect = false;
        effect.PlayEffect();
    }

    public void AddKill()
    {
        kills++;
        killsTypewriter.ShowText(kills.ToString());
        killsTypewriter.SkipTypewriter();
    }

    public void SetScores(int _wins, int _kills)
    {
        wins = _wins;
        kills = _kills;

        winsText.text = wins.ToString();
        killsText.text = kills.ToString();

        int visibleWins = wins % pointBubbles.Length;

        for (int i = 0; i < pointBubbles.Length; i++)
        {
            pointBubbles[i].GetComponent<PointBubbleResizeEffect>().HasPerformedEffect = true;
            pointBubbles[i].enabled = i < visibleWins;
        }
    }
}