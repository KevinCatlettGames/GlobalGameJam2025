#if !UNITY_SWITCH
using Steamworks;
#endif
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerProfileDisplay : MonoBehaviour
{
    [SerializeField] private GameObject avatarMask;
    [SerializeField] private RawImage avatarDisplay;
    public Texture2D cachedAvatar;
    [SerializeField] bool showOnEnable;
#if !UNITY_SWITCH
    public async void ShowSteamAvatarBySteamID(ulong steamID)
    {
        if (!TransportSwitcher.Instance.isUsingRelay || !SteamClient.IsValid) return;

        try
        {
            var steamImage = await SteamAvatarLoader.GetSteamAvatarAsync(steamID);

            if (steamImage.HasValue)
            {
                cachedAvatar = steamImage.Value.ConvertToTexture2D();

                if (avatarDisplay != null)
                {
                    avatarMask.SetActive(true);
                    avatarDisplay.texture = cachedAvatar;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"[SteamAvatar] Exception caught during fetch/conversion: {ex.Message}\n{ex.StackTrace}");
        }
    }

    public void HideSteamAvatar()
    {
        avatarMask.SetActive(false);            
    }

    private void OnDestroy()
    {
        if (cachedAvatar != null)
        {
            Destroy(cachedAvatar);
        }
    }
#endif
}