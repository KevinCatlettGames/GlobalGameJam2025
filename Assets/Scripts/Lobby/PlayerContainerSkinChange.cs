using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerContainerSkinChange : NetworkBehaviour
{
    public Image containerBackground;
    public Outline containerBackgroundOutline;
    public Image avatarBackground;
    public Image avatar;

    public int currentColorIndex;
    public int playerIndex = 0;
    
    public bool currentlyOnLocked;

    public void SwapColorWithIncrementation(bool increment)
    {
        if (LobbyManager.instance != null && LobbyManager.instance.players[playerIndex].IsReady) return; 
        
        int totalSkins = LobbyManager.instance.possibleSkins.Length;

        currentColorIndex = increment
            ? (currentColorIndex + 1) % totalSkins
            : (currentColorIndex - 1 + totalSkins) % totalSkins;

        UpdateSkin();
    }

    // This RPC ensures that the skin updates across all clients
    [ServerRpc(RequireOwnership = false)]
    public void UpdateSkinServerRpc()
    {
        UpdateSkin();
        UpdateSkinClientRpc(); // Send updated skin info to all clients
    }

    [ClientRpc]
    void UpdateSkinClientRpc()
    {
        UpdateSkin();
    }

    // This function handles both the skin change and checking if the skin is locked
    public void UpdateSkin()
    {
        SkinSO skinToUse = LobbyManager.instance.possibleSkins[currentColorIndex];
        bool skinLocked = false;

        // Check if any other player has the same skin and is ready
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

        // Update the lock status
        currentlyOnLocked = skinLocked;

        if (!currentlyOnLocked && playerIndex >= 0 && playerIndex < LobbyPlayerHandler.Instance.playerValues.Count)
        {
            LobbyPlayerHandler.Instance.playerValues[playerIndex].Skin = skinToUse;
        }

        ApplySkinVisuals();
    }

    // Apply the visuals based on the lock state
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
