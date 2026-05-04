using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using FMODUnity;
using System.Collections.Generic;
using System;

public class PlayerContainerSkinChange : NetworkBehaviour
{
    [SerializeField] private Image avatar;
    [SerializeField] private Image playerTextImage;
    [SerializeField] private StudioEventEmitter cycleEmitter;

    public int currentColorIndex;
    public int playerIndex = 0;
    public bool currentlyOnLocked;

    public SkinButtonHandler[] allSkinSelections;
    public SkinButtonHandler currentSkinSelection;
    bool init;
    bool wasInit = false; 

    private void OnDisable()
    {
        if (LobbyManager.instance != null)
            LobbyManager.instance.OnReadyStateUpdated.RemoveListener(ReadyStateUpdated);

        SkinSO skinToUse = LobbyManager.instance.PossibleSkins[playerIndex];
       
        avatar.sprite = skinToUse.LobbySprite;
        avatar.color = Color.white;
        gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (LobbyManager.instance != null)
            LobbyManager.instance.OnReadyStateUpdated.AddListener(ReadyStateUpdated);

        if (init && !wasInit)
        {
            SkinButtonHandler availableSkin = null;

            foreach (SkinButtonHandler skinButtonHandler in allSkinSelections)
            {
                if (skinButtonHandler != currentSkinSelection && !skinButtonHandler.isSelected)
                {
                    availableSkin = skinButtonHandler;
                    break;
                }
            }
            currentSkinSelection = availableSkin;
            currentSkinSelection.isSelected = true;
            currentSkinSelection.TogglePlayerIcon(true, playerIndex);
            currentColorIndex = currentSkinSelection.skinSo.Index;
            UpdateSkin();
            wasInit = true;
        }
        init = true;
    }

    public void ReadyStateUpdated(ulong clientId)
    {
        if ((int)clientId != playerIndex) return;
        currentSkinSelection.ToggleReadyVisuals();
    }

    public void SwapColorWithIncrementation(bool increment)
    {
        if (LobbyManager.instance != null && LobbyManager.instance.players[playerIndex].IsReady) return;

        int totalSkins = LobbyManager.instance.PossibleSkins.Length;
        currentColorIndex = increment
            ? (currentColorIndex + 1) % totalSkins
            : (currentColorIndex - 1 + totalSkins) % totalSkins;

        UpdateSkin();
        cycleEmitter.Play();
    }

    public void ChangeSkin(Vector2 skinChangeInput)
    {
        if(skinChangeInput.x == 0 && skinChangeInput.y == 0)
        {
            SkinButtonHandler availableSkin = null;

            foreach (SkinButtonHandler skinButtonHandler in allSkinSelections)
            {
                if (skinButtonHandler != currentSkinSelection && !skinButtonHandler.isSelected)
                {
                    availableSkin = skinButtonHandler;
                    break;
                }
            }
            currentSkinSelection.isSelected = false;
            currentSkinSelection.TogglePlayerIcon(false, playerIndex);
            currentSkinSelection = availableSkin;
            currentSkinSelection.isSelected = true;
            currentColorIndex = currentSkinSelection.skinSo.Index;
            UpdateSkin();
        }
        else if (skinChangeInput.x > 0)
        {
            if (!currentSkinSelection.rightSkinSelection) return;
            currentSkinSelection.isSelected = false;
            SkinButtonHandler availableSkin = null; 
            SkinButtonHandler skinToCheck = currentSkinSelection.rightSkinSelection;
            bool checkThroughNeighbor = true;

            var visited = new HashSet<SkinButtonHandler>();

            while (checkThroughNeighbor)
            {
                if (visited.Contains(skinToCheck))
                {
                    break;
                }

                if (skinToCheck.isSelected)
                {
                    if(skinToCheck.rightSkinSelection == null)
                    {         
                        checkThroughNeighbor = false;                      
                    }
                    else
                    {
                        skinToCheck = skinToCheck.rightSkinSelection;
                    }
                }
                else
                {
                    availableSkin = skinToCheck;
                    checkThroughNeighbor = false;
                }
            }

            if (availableSkin == null)
            {
                foreach (SkinButtonHandler skinButtonHandler in allSkinSelections)
                {
                    if (skinButtonHandler != currentSkinSelection && !skinButtonHandler.isSelected)
                    {
                        availableSkin = skinButtonHandler;
                    }
                }
            }
            currentSkinSelection.TogglePlayerIcon(false, playerIndex);
            currentSkinSelection = availableSkin;
            currentSkinSelection.isSelected = true;
            currentColorIndex = currentSkinSelection.skinSo.Index;
            UpdateSkin();
        }
        else if (skinChangeInput.x < 0)
        {
            if (!currentSkinSelection.leftSkinSelection) return;
            currentSkinSelection.isSelected = false;
            SkinButtonHandler availableSkin = null;
            SkinButtonHandler skinToCheck = currentSkinSelection.leftSkinSelection;
            bool checkThroughNeighbor = true;

            var visited = new HashSet<SkinButtonHandler>();

            while (checkThroughNeighbor)
            {
                if (visited.Contains(skinToCheck))
                {
                    break;
                }

                if (skinToCheck.isSelected)
                {
                    if (skinToCheck.leftSkinSelection == null)
                    {
                        checkThroughNeighbor = false;
                    }
                    else
                    {
                        skinToCheck = skinToCheck.leftSkinSelection;
                    }
                }
                else
                {
                    availableSkin = skinToCheck;
                    checkThroughNeighbor = false;
                }
            }

            if (availableSkin == null)
            {
                foreach (SkinButtonHandler skinButtonHandler in allSkinSelections)
                {
                    if (skinButtonHandler != currentSkinSelection && !skinButtonHandler.isSelected)
                    {
                        availableSkin = skinButtonHandler;
                    }
                }
            }           
            currentSkinSelection.TogglePlayerIcon(false, playerIndex);
            currentSkinSelection = availableSkin;
            currentSkinSelection.isSelected = true;
            currentColorIndex = currentSkinSelection.skinSo.Index;
            UpdateSkin();
        }
        else if (skinChangeInput.y > 0)
        {
            if (!currentSkinSelection.topSkinSelection) return;
            currentSkinSelection.isSelected = false;
            SkinButtonHandler availableSkin = null;
            SkinButtonHandler skinToCheck = currentSkinSelection.topSkinSelection;
            bool checkThroughNeighbor = true;

            var visited = new HashSet<SkinButtonHandler>();

            while (checkThroughNeighbor)
            {
                if (visited.Contains(skinToCheck))
                {
                    break;
                }
                if (skinToCheck.isSelected)
                {
                    if (skinToCheck.topSkinSelection == null)
                    {
                        checkThroughNeighbor = false;
                    }
                    else
                    {
                        skinToCheck = skinToCheck.topSkinSelection;
                    }
                }
                else
                {
                    availableSkin = skinToCheck;
                    checkThroughNeighbor = false;
                }
            }

            if (availableSkin == null)
            {
                foreach (SkinButtonHandler skinButtonHandler in allSkinSelections)
                {
                    if (skinButtonHandler != currentSkinSelection && !skinButtonHandler.isSelected)
                    {
                        availableSkin = skinButtonHandler;
                    }
                }
            }
            currentSkinSelection.TogglePlayerIcon(false, playerIndex);
            currentSkinSelection = availableSkin;
            currentSkinSelection.isSelected = true;
            currentColorIndex = currentSkinSelection.skinSo.Index;
            UpdateSkin();
        }
        else if (skinChangeInput.y < 0)
        {
            if (!currentSkinSelection.bottomSkinSelection) return;
            currentSkinSelection.isSelected = false;
            SkinButtonHandler availableSkin = null;
            SkinButtonHandler skinToCheck = currentSkinSelection.bottomSkinSelection;
            bool checkThroughNeighbor = true;

            var visited = new HashSet<SkinButtonHandler>();

            while (checkThroughNeighbor)
            {
                if (visited.Contains(skinToCheck))
                {
                    break;
                }
                if (skinToCheck.isSelected)
                {
                    if (skinToCheck.bottomSkinSelection == null)
                    {
                        checkThroughNeighbor = false;
                    }
                    else
                    {
                        skinToCheck = skinToCheck.bottomSkinSelection;
                    }
                }
                else
                {
                    availableSkin = skinToCheck;
                    checkThroughNeighbor = false;
                }
            }

            if (availableSkin == null)
            {
                foreach (SkinButtonHandler skinButtonHandler in allSkinSelections)
                {
                    if (skinButtonHandler != currentSkinSelection && !skinButtonHandler.isSelected)
                    {
                        availableSkin = skinButtonHandler;
                    }
                }
            }
            currentSkinSelection.TogglePlayerIcon(false, playerIndex);
            currentSkinSelection = availableSkin;
            currentSkinSelection.isSelected = true;
            currentColorIndex = currentSkinSelection.skinSo.Index;
            UpdateSkin();            
        }
    }


    [ServerRpc(RequireOwnership = false)]
    public void UpdateSkinServerRpc()
    {
        UpdateSkin();
        UpdateSkinClientRpc();
    }

    [ClientRpc]
    private void UpdateSkinClientRpc()
    {
        UpdateSkin();
    }

    public void UpdateSkin()
    {
        SkinSO skinToUse = LobbyManager.instance.PossibleSkins[currentColorIndex];
        bool skinLocked = false;

        for (int i = 0; i < LobbyPlayerValues.Instance.playerValuesList.Count; i++)
        {
            if (i == playerIndex) continue;

            var otherSkin = LobbyPlayerValues.Instance.playerValuesList[i].Skin;
            if (otherSkin != null && otherSkin == skinToUse &&
                i < LobbyManager.instance.players.Count &&
                LobbyManager.instance.players[i].IsReady)
            {
                skinLocked = true;
                break;
            }
        }

        currentlyOnLocked = skinLocked;

        if (!currentlyOnLocked &&
            playerIndex >= 0 &&
            playerIndex < LobbyPlayerValues.Instance.playerValuesList.Count)
        {
            LobbyPlayerValues.Instance.playerValuesList[playerIndex].Skin = skinToUse;
        }

        ApplySkinVisuals();
    }

    private void ApplySkinVisuals()
    {
        SkinSO skinToUse = LobbyManager.instance.PossibleSkins[currentColorIndex];

        if (currentlyOnLocked)
        {
            avatar.sprite = skinToUse.LobbySprite;
            avatar.color = Color.gray;
            if(currentSkinSelection)
            currentSkinSelection.TogglePlayerIcon(false, playerIndex);
        }
        else
        {
            playerTextImage.color = skinToUse.Color;
            avatar.sprite = skinToUse.LobbySprite;
            avatar.color = Color.white;
            if(currentSkinSelection)
                currentSkinSelection.TogglePlayerIcon(true, playerIndex);
        }
    }
}