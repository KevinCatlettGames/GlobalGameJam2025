using System;
using System.Threading.Tasks;
using UnityEngine;
using Steamworks;
using Steamworks.Data;

public static class SteamAvatarLoader
{
    // 1. Fetches the raw Steam image asynchronously
    public static async Task<Steamworks.Data.Image?> GetSteamAvatarAsync()
    {
        try
        {
            // Ensure Steam is initialized before calling
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

            // Fetch the avatar for the specified Steam ID instead of the local client
            return await SteamFriends.GetLargeAvatarAsync(steamId);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to fetch Steam avatar for {steamId}: {e.Message}");
            return null;
        }
    }

    // 2. Extension method to safely convert Steam Image to Unity Texture2D
    // Fixed the off-by-one bug here: (int)image.Height - 1 - y
    public static Texture2D ConvertToTexture2D(this Steamworks.Data.Image image)
    {
        // Create a new Texture2D matching the Steam image dimensions
        Texture2D avatarTexture = new Texture2D((int)image.Width, (int)image.Height, TextureFormat.ARGB32, false);

        // Set filter mode to prevent blurring
        avatarTexture.filterMode = FilterMode.Trilinear;

        // Loop through pixels and flip vertically (Steam uses top-left origin, Unity uses bottom-left)
        for (int x = 0; x < image.Width; x++)
        {
            for (int y = 0; y < image.Height; y++)
            {
                var pixel = image.GetPixel(x, y);

                // Convert 0-255 byte values to Unity's 0.0f - 1.0f Color range
                UnityEngine.Color unityColor = new UnityEngine.Color(
                    pixel.r / 255.0f,
                    pixel.g / 255.0f,
                    pixel.b / 255.0f,
                    pixel.a / 255.0f
                );

                // CRITICAL FIX: Added "- 1" to avoid an ArgumentOutOfRangeException on row 0
                int flippedY = (int)image.Height - 1 - y;

                avatarTexture.SetPixel(x, flippedY, unityColor);
            }
        }

        // Upload data to the GPU
        avatarTexture.Apply();
        return avatarTexture;
    }
}