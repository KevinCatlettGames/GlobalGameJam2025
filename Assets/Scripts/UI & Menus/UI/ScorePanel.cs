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
    [SerializeField] private TypewriterByWord teamAKillsTypewriter;
    [SerializeField] private TypewriterByWord teamBKillsTypewriter;

    [SerializeField] private TextMeshProUGUI winsText;
    [SerializeField] private TextMeshProUGUI killsText;
    [SerializeField] private TextMeshProUGUI teamAKillsText;
    [SerializeField] private TextMeshProUGUI teamBKillsText;

    [SerializeField] private Image winsImage;
    [SerializeField] private Image killsImage;
    [SerializeField] private Image teamAKillsImage;
    [SerializeField] private Image teamBKillsImage;

    [SerializeField] private Image portrait;
    [SerializeField] private Image[] pointBubbles;
    [SerializeField] private Image frame;
    [SerializeField] private Color colorShift;

    [SerializeField] private bool hideElementsOnTeamMode;
    [SerializeField] private bool useAsSecondTeamMember;
    [SerializeField] private int scorePanelOffset = 0;
    [SerializeField] private float scoreOffset = 0;
    [SerializeField] private int playerID = -1; 
    [SerializeField] public int teamIndex = -1;
    private bool initialSet = false;
    
    private int kills = 0;
    private int wins = 0;

    private void OnEnable()
    {
        if (GameManager.Instance.GameMode != GameManager.GameModeType.Team || initialSet) return;

        initialSet = true; 
        
        teamIndex = GameManager.Instance.TeamIDs[playerID];
        portrait.enabled = false;
        
        if (hideElementsOnTeamMode)
        {
            killsImage.enabled = false;
            killsText.enabled = false;
            winsText.enabled = false;
            winsImage.enabled = false;
            frame.enabled = false;
            foreach(Image image in pointBubbles)
                image.enabled = false;

            return;
        }
        
        killsImage.rectTransform.position = new Vector3(killsImage.rectTransform.position.x, killsImage.rectTransform.position.y + scoreOffset, killsImage.rectTransform.position.z);
        killsText.rectTransform.position = new Vector3(killsText.rectTransform.position.x, killsText.rectTransform.position.y + scoreOffset, killsText.rectTransform.position.z);
        winsImage.rectTransform.position = new Vector3(winsImage.rectTransform.position.x, winsImage.rectTransform.position.y + scoreOffset, winsImage.rectTransform.position.z);
        winsText.rectTransform.position = new Vector3(winsText.rectTransform.position.x, winsText.rectTransform.position.y + scoreOffset, winsText.rectTransform.position.z);
        
        gameObject.transform.position = new Vector3(gameObject.transform.position.x, gameObject.transform.position.y + scorePanelOffset, gameObject.transform.position.z);
    }

    public void SetPortrait(Sprite playerPortrait, Color playerColor)
    {
        portrait.sprite = playerPortrait;

        if (GameManager.Instance.GameMode == GameManager.GameModeType.Team) return; 
        
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

        if (useAsSecondTeamMember && teamIndex == 1 && teamAKillsText.gameObject.activeSelf)
        {
            teamAKillsTypewriter.ShowText(kills.ToString());
            teamAKillsTypewriter.SkipTypewriter();
        }
        if (useAsSecondTeamMember && teamIndex == 2 && teamBKillsText.gameObject.activeSelf)
        {
            teamBKillsTypewriter.ShowText(kills.ToString());
            teamBKillsTypewriter.SkipTypewriter();
        }
    }

    public void SetScores(int _wins, int _kills)
    {
        wins = _wins;
        kills = _kills;

        winsText.text = wins.ToString();
        killsText.text = kills.ToString();
        
        if(useAsSecondTeamMember && teamIndex == 1)
            teamAKillsText.text = kills.ToString();
        if(useAsSecondTeamMember && teamIndex == 2)
            teamBKillsText.text = kills.ToString();
        
        int visibleWins = wins % pointBubbles.Length;

        for (int i = 0; i < pointBubbles.Length; i++)
        {
            pointBubbles[i].GetComponent<PointBubbleResizeEffect>().HasPerformedEffect = true;
            pointBubbles[i].enabled = i < visibleWins;
        }
    }
}