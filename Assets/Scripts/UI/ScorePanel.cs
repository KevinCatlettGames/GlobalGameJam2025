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
    private int kills = 0;
    private int wins = 0;

    void Start()
    {
        killsTypewriter.ShowText(kills.ToString());
        winsTypewriter.ShowText(wins.ToString());
    }

    public void SetPortrait(Sprite playerPortrait)
    {
        portrait.sprite = playerPortrait;
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
        wins = _wins;
        //winsTypewriter.ShowText(kills.ToString());
        //winsTypewriter.SkipTypewriter();
        winsText.text = wins.ToString();
        kills = _kills;
        killsText.text = kills.ToString();
    }
}
