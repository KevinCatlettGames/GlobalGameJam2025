using System;
using System.Threading.Tasks;
using UnityEngine;
using Steamworks;
using Steamworks.Data;

public static class SteamAvatarLoader
{
    public static async Task<Steamworks.Data.Image?> GetSteamAvatarAsync()
    {
        try
        {
            if (!SteamClient.IsValid)
            {
                Debug.LogWarning("SteamClient is not initialized.");
                return null;
            }

            return await SteamFriends.GetLargeAvatarAsync(SteamClient.SteamId);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to fetch Steam avatar: {e.Message}");
            return null;
        }
    }
    public static async Task<Steamworks.Data.Image?> GetSteamAvatarAsync(ulong steamId)
    {
        try
        {
            if (!SteamClient.IsValid)
            {
                Debug.LogWarning("SteamClient is not initialized.");
                return null;
            }

            return await SteamFriends.GetLargeAvatarAsync(steamId);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to fetch Steam avatar for {steamId}: {e.Message}");
            return null;
        }
    }

    public static Texture2D ConvertToTexture2D(this Steamworks.Data.Image image)
    {
        Texture2D avatarTexture = new Texture2D((int)image.Width, (int)image.Height, TextureFormat.ARGB32, false);

        avatarTexture.filterMode = FilterMode.Trilinear;

        for (int x = 0; x < image.Width; x++)
        {
            for (int y = 0; y < image.Height; y++)
            {
                var pixel = image.GetPixel(x, y);

                UnityEngine.Color unityColor = new UnityEngine.Color(
                    pixel.r / 255.0f,
                    pixel.g / 255.0f,
                    pixel.b / 255.0f,
                    pixel.a / 255.0f
                );

                int flippedY = (int)image.Height - 1 - y;

                avatarTexture.SetPixel(x, flippedY, unityColor);
            }
        }

        avatarTexture.Apply();
        return avatarTexture;
    }
}