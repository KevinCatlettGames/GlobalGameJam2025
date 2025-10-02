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
    [SerializeField] private Image background;
    [SerializeField] private Image frame;
    [SerializeField] private Color colorShift;
    private int kills = 0;
    private int wins = 0;

    void Start()
    {
        //killsTypewriter.ShowText(kills.ToString());
        //winsTypewriter.ShowText(wins.ToString());
    }

    public void SetPortrait(Sprite playerPortrait, Color playerColor)
    {
        portrait.sprite = playerPortrait;
        background.color = playerColor;
        Color frameColor = playerColor - colorShift;
        frame.color = frameColor;
    }

    public void AddWin()
    {
        wins++;
        winsTypewriter.ShowText(wins.ToString());
    }

    public void AddKill()
    {
        kills++;
        killsTypewriter.ShowText(kills.ToString());
        killsTypewriter.SkipTypewriter();
    }

    public void SetScores(int _wins, int _kills)
    {
        //REMOVE LATER!!!!!!
        return;
        //REMOVE LATER!!!!!!
        wins = _wins;
        //winsTypewriter.ShowText(kills.ToString());
        //winsTypewriter.SkipTypewriter();
        winsText.text = wins.ToString();
        kills = _kills;
        killsText.text = kills.ToString();
    }
}
