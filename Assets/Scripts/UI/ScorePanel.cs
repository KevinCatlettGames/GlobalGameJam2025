using Febucci.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


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
        Color frameColor = playerColor - colorShift;
        frame.color = frameColor;
    }

    public void AddWin()
    {
        wins++;

        for (int i = 0; i < pointBubbles.Length; i++)
            pointBubbles[i].enabled = i < wins;
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
        for (int i = 0; i < pointBubbles.Length; i++)
            pointBubbles[i].enabled = i < wins;
        
        kills = _kills;
        killsText.text = kills.ToString();
    }
}
