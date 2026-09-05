using Febucci.UI;
using System.Collections.Generic;
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

    [SerializeField] private Image killsIcon;
    [SerializeField] private Image[] portraits;
    [SerializeField] private Image[] pointBubbles;
    [SerializeField] private Image[] fakePointBubbles;
    [SerializeField] private Image frame;
    [SerializeField] private Color colorShift;

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

        killsIcon.enabled = false;   
        killsText.enabled = false;


        if(LobbyManager.instance && !LobbyManager.instance.playEndless)
        {
            foreach(Image image in fakePointBubbles)
                image.enabled = false;
            for (int i = 0; i < LobbyManager.instance.winsNeeded; i++)
            {
                if(fakePointBubbles.Length > i)
                    fakePointBubbles[i].enabled = true;
            }
        }
    }

    private void OnDisable()
    {
        foreach (Image image in portraits)
            image.enabled = false;
    }

    public void SetPortrait(Sprite playerPortrait, Color playerColor, int portraitID)
    {
        portraits[portraitID].enabled = playerPortrait != null;
        portraits[portraitID].sprite = playerPortrait;

        foreach (Image image in pointBubbles)
        {
            image.enabled = true;
            //image.color = playerColor;
        }

        frame.color = playerColor - colorShift;
    }

    public void SetTeamPortraits(Sprite playerPortrait)
    {
        if (portraitsSet >= portraits.Length)
            return;

        portraits[portraitsSet].sprite = playerPortrait;
        portraits[portraitsSet].enabled = true;

        portraitsSet++;
    }

    public void AddWin()
    {
        wins++;

        //winsTypewriter.ShowText(wins.ToString());
        //winsTypewriter.SkipTypewriter();

        int visibleWins = GetVisibleWins(wins, pointBubbles.Length);
        UpdateBubbles(visibleWins, true);
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
        killsText.enabled = true;
        killsIcon.enabled = true;

        int visibleWins = GetVisibleWins(wins, pointBubbles.Length);
        UpdateBubbles(visibleWins, false);
    }

    public void SetTeamScores(int _wins, int _kills)
    {
        wins = _wins;
        winsText.text = wins.ToString();

        killsText.enabled = true;
        killsIcon.enabled = true;

        killsText.text = _kills.ToString();

        int visibleWins = GetVisibleWins(wins, pointBubbles.Length);
        UpdateBubbles(visibleWins, false);
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