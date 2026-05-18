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
    [SerializeField] private Image blurImage;
    [SerializeField] private float blurImageAlpha = .1f;
    Color initialBlurColor;
    [SerializeField] private StudioEventEmitter cycleEmitter;

    public int currentColorIndex;
    public int playerIndex = 0;
    public bool currentlyOnLocked;
    [SerializeField] private TeamSelection teamSelection;
    public SkinButtonHandler[] allSkinSelections;
    public SkinButtonHandler currentSkinSelection;
    bool init;
    bool wasInit = false;
    public GameObject emptyPlayerContainer;

    private void OnDisable()
    {
        if (LobbyManager.instance != null)
            LobbyManager.instance.OnReadyStateUpdated.RemoveListener(ReadyStateUpdated);
    }

    private void Awake()
    {
        initialBlurColor = blurImage.color;
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
            avatar.GetComponent<ScaleToCorrectSize>().Play();
            UpdateBlur();
            UpdateSkin();
            wasInit = true;
        }
        init = true;
    }

    public void ReadyStateUpdated(ulong clientId, bool state)
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

    public void ResetContainer()
    {
        currentSkinSelection.ResetVisuals();
        currentSkinSelection = null;
        blurImage.color = initialBlurColor;
        emptyPlayerContainer.SetActive(true);
        wasInit = false;
        init = true;
        gameObject.SetActive(false);
    }

    public void ChangeSkin(Vector2 skinChangeInput)
    {
        SkinButtonHandler availableSkin = null;

        if (skinChangeInput.x == 0 && skinChangeInput.y == 0)
        {
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
            avatar.GetComponent<ScaleToCorrectSize>().Play();
            UpdateBlur();
            UpdateSkin();
        }
        else if (skinChangeInput.x > 0)
        {
            if (!currentSkinSelection.rightSkinSelection || !SteamIntegration.instance.IsFullVersion && !currentSkinSelection.rightSkinSelection.skinSo.AvailableInDemo) return;
            currentSkinSelection.isSelected = false;
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
                    if(skinToCheck.rightSkinSelection == null || !SteamIntegration.instance.IsFullVersion && !skinToCheck.rightSkinSelection.skinSo.AvailableInDemo) 
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
        }
        else if (skinChangeInput.x < 0)
        {
            if (!currentSkinSelection.leftSkinSelection || !SteamIntegration.instance.IsFullVersion && !currentSkinSelection.leftSkinSelection.skinSo.AvailableInDemo) return;
            currentSkinSelection.isSelected = false;
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
                    if (skinToCheck.leftSkinSelection == null || !SteamIntegration.instance.IsFullVersion && !skinToCheck.leftSkinSelection.skinSo.AvailableInDemo)
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
        }
        else if (skinChangeInput.y > 0)
        {
            if (!currentSkinSelection.topSkinSelection || !SteamIntegration.instance.IsFullVersion && !currentSkinSelection.topSkinSelection.skinSo.AvailableInDemo) return;
            currentSkinSelection.isSelected = false;
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
                    if (skinToCheck.topSkinSelection == null || !SteamIntegration.instance.IsFullVersion && !skinToCheck.topSkinSelection.skinSo.AvailableInDemo)
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
        }
        else if (skinChangeInput.y < 0)
        {
            if (!currentSkinSelection.bottomSkinSelection || !SteamIntegration.instance.IsFullVersion && !currentSkinSelection.bottomSkinSelection.skinSo.AvailableInDemo) return;
            currentSkinSelection.isSelected = false;          
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
                    if (skinToCheck.bottomSkinSelection == null || !SteamIntegration.instance.IsFullVersion && !skinToCheck.bottomSkinSelection.skinSo.AvailableInDemo)
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
        }

        if (availableSkin == null)
        {
            foreach (SkinButtonHandler skinButtonHandler in allSkinSelections)
            {
                if (!SteamIntegration.instance.IsFullVersion && !skinButtonHandler.skinSo.AvailableInDemo) return;
                if (skinButtonHandler != currentSkinSelection && !skinButtonHandler.isSelected)
                {
                    availableSkin = skinButtonHandler;
                }
            }
        }
        if (availableSkin == null) return; 

        currentSkinSelection.TogglePlayerIcon(false, playerIndex);
        currentSkinSelection = availableSkin;
        currentSkinSelection.isSelected = true;
        currentColorIndex = currentSkinSelection.skinSo.Index;
        avatar.GetComponent<ScaleToCorrectSize>().Play();
        UpdateBlur();
        UpdateSkin();
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
        if(!gameObject.activeSelf) return;

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
            if (currentSkinSelection)
            {
                currentSkinSelection.TogglePlayerIcon(true, playerIndex);
            }
        }     
    }

    public void UpdateBlur()
    {
        blurImage.enabled = true;
        Color c = Color.white;

        if (LobbyManager.instance.SelectedGameMode == GameManager.GameModeType.Standard)
            c = currentSkinSelection.skinSo.Color;
        else if (LobbyManager.instance.SelectedGameMode == GameManager.GameModeType.Team)
        {
            if (teamSelection.CurrentTeamIndex == 1)
                c = LobbyManager.instance.TeamColors[0];
            else if (teamSelection.CurrentTeamIndex == 2)
                c = LobbyManager.instance.TeamColors[1];
        }

        c.a = blurImageAlpha;
        blurImage.color = c;
    }
}