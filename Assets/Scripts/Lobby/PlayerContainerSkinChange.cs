using System;
using UnityEngine;
using UnityEngine.UI;

public class PlayerContainerSkinChange : MonoBehaviour
{
    public Image containerBackground;
    public Outline containerBackgroundOutline;
    public Image avatarBackground;
    public Image avatar;

    public int currentColorIndex; 
    public int playerIndex = 0;
    public bool currentlyOnLocked = false;

    private void Awake()
    {
        ApplySkinVisuals();
    }

    public void SwapColorWithIncrementation(bool increment)
    {
        int totalSkins = LobbyManager.instance.possibleSkins.Length;

        currentColorIndex = increment
            ? (currentColorIndex + 1) % totalSkins
            : (currentColorIndex - 1 + totalSkins) % totalSkins;

        UpdateSkin();
    }

    public void UpdateSkin()
    {
        SkinSO skinToUse = LobbyManager.instance.possibleSkins[currentColorIndex];
        bool skinLocked = false;

        for (int i = 0; i < LobbyPlayerHandler.Instance.playerValues.Count; i++)
        {
            if (i == playerIndex) continue;

            var otherSkin = LobbyPlayerHandler.Instance.playerValues[i].Skin;

            if (otherSkin != null && otherSkin == skinToUse &&
                i < LobbyManager.instance.players.Count &&
                LobbyManager.instance.players[i].IsReady)
            {
                skinLocked = true;
                break;
            }
        }

        currentlyOnLocked = skinLocked;

        if (!currentlyOnLocked && playerIndex >= 0 && playerIndex < LobbyPlayerHandler.Instance.playerValues.Count)
        {
            LobbyPlayerHandler.Instance.playerValues[playerIndex].Skin = skinToUse;
        }

        ApplySkinVisuals();
    }

    public void RecheckSkinValidity()
    {
        UpdateSkin();
    }

    private void ApplySkinVisuals()
    {
        SkinSO skinToUse = LobbyManager.instance.possibleSkins[currentColorIndex];

        if (currentlyOnLocked)
        {
            containerBackground.color = Color.gray;
            containerBackgroundOutline.effectColor = Color.gray;
            avatarBackground.color = Color.gray;
            avatar.sprite = skinToUse.Sprite;
            avatar.color = Color.gray;
        }
        else
        {
            containerBackground.color = skinToUse.Color;
            containerBackgroundOutline.effectColor = skinToUse.Color;
            avatarBackground.color = skinToUse.Color;
            avatar.sprite = skinToUse.Sprite;
            avatar.color = Color.white;
        }
    }
}
