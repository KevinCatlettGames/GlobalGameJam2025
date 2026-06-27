using UnityEngine;

public static class SaveManager
{
#if UNITY_SWITCH && !UNITY_EDITOR

    private const string MountName = "PlayerPrefsSave";
    private const string FileName = "PlayerPrefs.dat";
    private static readonly string FilePath = $"{MountName}:/{FileName}";

    private static nn.account.Uid userId;
    private static bool initialized;

#endif

    /// <summary>
    /// Call once during game startup.
    /// </summary>
    public static void Initialize()
    {
#if UNITY_SWITCH && !UNITY_EDITOR

        if (initialized)
            return;

        nn.account.Account.Initialize();

        nn.account.UserHandle userHandle =
            new nn.account.UserHandle();

        if (!nn.account.Account.TryOpenPreselectedUser(ref userHandle))
        {
            nn.Nn.Abort("Failed to open preselected user.");
        }

        nn.Result result =
            nn.account.Account.GetUserId(ref userId, userHandle);

        result.abortUnlessSuccess();

        result = nn.fs.SaveData.Mount(MountName, userId);
        result.abortUnlessSuccess();

        initialized = true;

        Load();

#endif
    }

    /// <summary>
    /// Saves PlayerPrefs.
    /// Use this instead of PlayerPrefs.Save().
    /// </summary>
    public static void Save()
    {
#if UNITY_SWITCH && !UNITY_EDITOR

        if (!initialized)
            Initialize();

        PlayerPrefs.Save();

        byte[] data = UnityEngine.Switch.PlayerPrefsHelper.rawData;

        UnityEngine.Switch.Notification
            .EnterExitRequestHandlingSection();

        try
        {
            nn.fs.EntryType entryType = 0;

            nn.Result result =
                nn.fs.FileSystem.GetEntryType(
                    ref entryType,
                    FilePath);

            if (nn.fs.FileSystem.ResultPathNotFound.Includes(result))
            {
                result = nn.fs.File.Create(
                    FilePath,
                    data.LongLength);

                result.abortUnlessSuccess();
            }

            nn.fs.FileHandle fileHandle =
                new nn.fs.FileHandle();

            result = nn.fs.File.Open(
                ref fileHandle,
                FilePath,
                nn.fs.OpenFileMode.Write);

            result.abortUnlessSuccess();

            result = nn.fs.File.SetSize(
                fileHandle,
                data.LongLength);

            result.abortUnlessSuccess();

            result = nn.fs.File.Write(
                fileHandle,
                0,
                data,
                data.LongLength,
                nn.fs.WriteOption.Flush);

            result.abortUnlessSuccess();

            nn.fs.File.Close(fileHandle);

            result = nn.fs.FileSystem.Commit(MountName);
            result.abortUnlessSuccess();
        }
        finally
        {
            UnityEngine.Switch.Notification
                .LeaveExitRequestHandlingSection();
        }

#else

        PlayerPrefs.Save();

#endif
    }

    /// <summary>
    /// Loads PlayerPrefs from Switch save data.
    /// </summary>
    public static void Load()
    {
#if UNITY_SWITCH && !UNITY_EDITOR

        if (!initialized)
            return;

        nn.fs.EntryType entryType = 0;

        nn.Result result =
            nn.fs.FileSystem.GetEntryType(
                ref entryType,
                FilePath);

        if (nn.fs.FileSystem.ResultPathNotFound.Includes(result))
            return;

        result.abortUnlessSuccess();

        nn.fs.FileHandle fileHandle =
            new nn.fs.FileHandle();

        result = nn.fs.File.Open(
            ref fileHandle,
            FilePath,
            nn.fs.OpenFileMode.Read);

        result.abortUnlessSuccess();

        long fileSize = 0;

        result = nn.fs.File.GetSize(
            ref fileSize,
            fileHandle);

        result.abortUnlessSuccess();

        byte[] data = new byte[fileSize];

        result = nn.fs.File.Read(
            fileHandle,
            0,
            data,
            fileSize);

        result.abortUnlessSuccess();

        nn.fs.File.Close(fileHandle);

        UnityEngine.Switch.PlayerPrefsHelper.rawData = data;

#endif
    }

    /// <summary>
    /// Call when shutting down.
    /// </summary>
    public static void Shutdown()
    {
#if UNITY_SWITCH && !UNITY_EDITOR

        if (!initialized)
            return;

        nn.fs.FileSystem.Unmount(MountName);

        initialized = false;

#endif
    }
}