using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using FMODUnity;

public class PlayerContainerSkinChange : NetworkBehaviour
{
    [SerializeField] private Image containerBackground;
    [SerializeField] private Outline containerBackgroundOutline;
    [SerializeField] private Image avatarBackground;
    [SerializeField] private Image avatar;
    [SerializeField] private StudioEventEmitter cycleEmitter;

    public int currentColorIndex;
    public int playerIndex = 0;
    public bool currentlyOnLocked;

    private void OnDisable()
    {
        SkinSO skinToUse = LobbyManager.instance.PossibleSkins[playerIndex];
        containerBackground.color = skinToUse.Color;
        containerBackgroundOutline.effectColor = skinToUse.Color;
        avatarBackground.color = skinToUse.Color;
        avatar.sprite = skinToUse.LobbySprite;
        avatar.color = Color.white;
        gameObject.SetActive(false);
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

        for (int i = 0; i < LobbyPlayerHandler.Instance.playerValuesList.Count; i++)
        {
            if (i == playerIndex) continue;

            var otherSkin = LobbyPlayerHandler.Instance.playerValuesList[i].Skin;
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
            playerIndex < LobbyPlayerHandler.Instance.playerValuesList.Count)
        {
            LobbyPlayerHandler.Instance.playerValuesList[playerIndex].Skin = skinToUse;
        }

        ApplySkinVisuals();
    }

    private void ApplySkinVisuals()
    {
        SkinSO skinToUse = LobbyManager.instance.PossibleSkins[currentColorIndex];

        if (currentlyOnLocked)
        {
            containerBackground.color = Color.gray;
            containerBackgroundOutline.effectColor = Color.gray;
            avatarBackground.color = Color.gray;
            avatar.sprite = skinToUse.LobbySprite;
            avatar.color = Color.gray;
        }
        else
        {
            containerBackground.color = skinToUse.Color;
            containerBackgroundOutline.effectColor = skinToUse.Color;
            avatarBackground.color = skinToUse.Color;
            avatar.sprite = skinToUse.LobbySprite;
            avatar.color = Color.white;
        }
    }
}