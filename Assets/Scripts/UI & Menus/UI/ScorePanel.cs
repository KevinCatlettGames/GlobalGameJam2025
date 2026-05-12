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
        if (GameManager.Instance.GameMode != GameManager.GameModeType.Team || initialSet)
            return;

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

            foreach (Image image in pointBubbles)
                image.enabled = false;

            return;
        }

        OffsetUI();
    }

    private void OffsetUI()
    {
        Vector3 offset = new Vector3(0, scoreOffset, 0);

        killsImage.rectTransform.position += offset;
        killsText.rectTransform.position += offset;
        winsImage.rectTransform.position += offset;
        winsText.rectTransform.position += offset;

        transform.position += new Vector3(0, scorePanelOffset, 0);
    }

    public void SetPortrait(Sprite playerPortrait, Color playerColor)
    {
        portrait.sprite = playerPortrait;

        if (GameManager.Instance.GameMode == GameManager.GameModeType.Team)
            return;

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

        int visibleWins = GetVisibleWins(wins, pointBubbles.Length);

        UpdateBubbles(visibleWins, playEffect: true);
    }

    public void AddKill()
    {
        kills++;

        killsTypewriter.ShowText(kills.ToString());
        killsTypewriter.SkipTypewriter();

        if (useAsSecondTeamMember)
        {
            if (teamIndex == 1 && teamAKillsText.gameObject.activeSelf)
            {
                teamAKillsTypewriter.ShowText(kills.ToString());
                teamAKillsTypewriter.SkipTypewriter();
            }
            else if (teamIndex == 2 && teamBKillsText.gameObject.activeSelf)
            {
                teamBKillsTypewriter.ShowText(kills.ToString());
                teamBKillsTypewriter.SkipTypewriter();
            }
        }
    }

    public void SetScores(int _wins, int _kills)
    {
        wins = _wins;
        kills = _kills;

        winsText.text = wins.ToString();
        killsText.text = kills.ToString();

        if (useAsSecondTeamMember)
        {
            if (teamIndex == 1)
                teamAKillsText.text = kills.ToString();
            else if (teamIndex == 2)
                teamBKillsText.text = kills.ToString();
        }

        int visibleWins = GetVisibleWins(wins, pointBubbles.Length);

        UpdateBubbles(visibleWins, playEffect: false);
    }

    private int GetVisibleWins(int wins, int bubbleCount)
    {
        if (wins <= 0)
            return 0;

        int result = wins % bubbleCount;
        return result == 0 ? bubbleCount : result;
    }

    private void UpdateBubbles(int visibleWins, bool playEffect)
    {
        for (int i = 0; i < pointBubbles.Length; i++)
        {
            var bubble = pointBubbles[i];
            var effect = bubble.GetComponent<ResizeEffect>();

            bubble.enabled = i < visibleWins;

            if (effect != null)
            {
                effect.HasPerformedEffect = !playEffect;

                if (playEffect && i == visibleWins - 1)
                {
                    effect.HasPerformedEffect = false;
                    effect.PlayEffect();
                }
            }
        }
    }
}