using UnityEngine;

public static class SaveManager
{
    /// <summary>
    /// Call once during game startup.
    /// </summary>
    public static void Initialize()
    {
    }

    /// <summary>
    /// Saves PlayerPrefs.
    /// Use this instead of PlayerPrefs.Save().
    /// </summary>
    public static void Save()
    {
        PlayerPrefs.Save();
    }

    /// <summary>
    /// Loads PlayerPrefs from Switch save data.
    /// </summary>
    public static void Load()
    {

    }

    /// <summary>
    /// Call when shutting down.
    /// </summary>
    public static void Shutdown()
    {

    }
}