using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkinButtonHandler : MonoBehaviour
{
    public SkinButtonHandler rightSkinSelection;
    public SkinButtonHandler leftSkinSelection;
    public SkinButtonHandler topSkinSelection;
    public SkinButtonHandler bottomSkinSelection;

    public Image[] selectionimages;
    public TextMeshProUGUI[] selectionTexts;

    public SkinSO skinSo;
    public Image skinImage;
    public Image shineImage;

    public Color standardImageColor = Color.gray;
    public Color disabledColor = Color.red;

    public Vector3 originalScale;
    public float scaleMultiplier;

    private int hoveredAmount = 0;
    private bool[] isHovering;

    private List<int> activePlayers = new List<int>();

    private void Awake()
    {
        originalScale = transform.localScale;
        isHovering = new bool[selectionimages.Length];
    }

    private void OnEnable()
    {
        foreach (Image image in selectionimages)
        {
            image.enabled = false;
            image.color = Color.white;
        }

        foreach (TextMeshProUGUI text in selectionTexts)
            text.enabled = false;

        if (!SteamIntegration.instance.IsFullVersion && !skinSo.AvailableInDemo)
            GetComponent<Image>().color = disabledColor;
        else
            GetComponent<Image>().color = standardImageColor;
    }

    public void ChangePlayerIcon(int amount, int playerIndex)
    {
        if (playerIndex < 0 || playerIndex >= selectionimages.Length)
            return;

        bool entering = amount > 0;

        if (isHovering[playerIndex] == entering)
            return;

        isHovering[playerIndex] = entering;

        if (entering)
        {
            if (!activePlayers.Contains(playerIndex))
                activePlayers.Add(playerIndex);
        }
        else
        {      
            activePlayers.Remove(playerIndex);
        }

        hoveredAmount = activePlayers.Count;

        RefreshUI();
    }

    private void RefreshUI()
    {
        for (int i = 0; i < selectionimages.Length; i++)
        {
            selectionimages[i].enabled = false;
            selectionTexts[i].enabled = false;
        }

        for (int slotIndex = 0; slotIndex < activePlayers.Count; slotIndex++)
        {
            int playerIndex = activePlayers[slotIndex];

            selectionimages[slotIndex].enabled = true;
            selectionimages[slotIndex].color = skinSo.Color;

            selectionTexts[slotIndex].enabled = true;
            selectionTexts[slotIndex].text = "P" + (playerIndex + 1);
        }

        bool hasHover = activePlayers.Count > 0;

        if (hasHover)
        {
            GetComponent<Outline>().effectColor = skinSo.Color;
            transform.localScale = originalScale * scaleMultiplier;
        }
        else
        {
            GetComponent<Outline>().effectColor = new Color(0, 0, 0, 0);
            transform.localScale = originalScale;
        }
    }

    public void ToggleReadyVisuals()
    {
        bool isSelectedNow = GetComponent<Image>().color != skinSo.Color;

        if (isSelectedNow)
        {
            GetComponent<Image>().color = skinSo.Color;
            shineImage.enabled = true;
        }
        else
        {
            GetComponent<Image>().color = standardImageColor;
            shineImage.enabled = false;
        }
    }

    public void ResetVisuals()
    {
        gameObject.GetComponent<Outline>().effectColor = new Color(0, 0, 0, 0);
        transform.localScale = originalScale;
        GetComponent<Image>().color = standardImageColor;

        foreach (Image image in selectionimages)
        {
            image.enabled = false;
            image.color = Color.white;
        }

        foreach (TextMeshProUGUI text in selectionTexts)
            text.enabled = false;

        shineImage.enabled = false;

        activePlayers.Clear();
        hoveredAmount = 0;

        for (int i = 0; i < isHovering.Length; i++)
            isHovering[i] = false;
    }
}