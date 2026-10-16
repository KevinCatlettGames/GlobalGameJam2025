using System;
using System.IO;
using UnityEngine;

public class FsSaveDataPlayerPrefs : MonoBehaviour
{
#if UNITY_SWITCH && !UNITY_EDITOR
    public static FsSaveDataPlayerPrefs instance = null;
    private bool hasInitialized = false;

    private static nn.account.Uid userId;
    private static nn.fs.FileHandle fileHandle = new nn.fs.FileHandle();

    private const string mountName = "WizzoPopOutSave";
    private const string fileName = "WizzoPopOutSaveData";
    private static readonly string filePath = string.Format("{0}:/{1}", mountName, fileName);

    private const string versionKey = "Version";
    private const int saveDataVersion = 1;

    private void Awake()
    {
        transform.parent = null;
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            this.Initialize();
        }
        else
        {
            hasInitialized = true;
            Destroy(gameObject);
        }
    }

    private void Initialize()
    {
        Console.WriteLine("FsSaveDataPlayerPrefs.Initialize()");
        if (!hasInitialized)
        {
            nn.account.Account.Initialize();
            nn.account.UserHandle userHandle = new nn.account.UserHandle();

            if (!nn.account.Account.TryOpenPreselectedUser(ref userHandle))
            {
                nn.Nn.Abort("Failed to open preselected user.");
            }
            nn.Result result = nn.account.Account.GetUserId(ref userId, userHandle);
            result.abortUnlessSuccess();
            result = nn.fs.SaveData.Mount(mountName, userId);
            result.abortUnlessSuccess();

            InitializeSaveData();
            Load();
            hasInitialized = true;
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
            nn.fs.FileSystem.Unmount(mountName);
        }
    }

    private void InitializeSaveData()
    {
        nn.fs.EntryType entryType = 0;
        nn.Result result = nn.fs.FileSystem.GetEntryType(ref entryType, filePath);
        if (result.IsSuccess())
        {
            Console.WriteLine("FsSaveDataPlayerPrefs.InitializeSaveData(): Save data already present, skipping initialization.");
            return;
        }

        Console.WriteLine("FsSaveDataPlayerPrefs.InitializeSaveData(): Creating new save file.");
        PlayerPrefs.SetInt(versionKey, saveDataVersion);
        PlayerPrefs.Save(); // Force Unity to sync to rawData before copying

        byte[] data = UnityEngine.Switch.PlayerPrefsHelper.rawData;
        long saveDataSize = data.LongLength;

        UnityEngine.Switch.Notification.EnterExitRequestHandlingSection();

        result = nn.fs.File.Create(filePath, saveDataSize);
        result.abortUnlessSuccess();

        result = nn.fs.File.Open(ref fileHandle, filePath, nn.fs.OpenFileMode.Write);
        result.abortUnlessSuccess();

        result = nn.fs.File.SetSize(fileHandle, saveDataSize);
        result.abortUnlessSuccess();

        result = nn.fs.File.Write(fileHandle, 0, data, saveDataSize, nn.fs.WriteOption.Flush);
        result.abortUnlessSuccess();

        nn.fs.File.Close(fileHandle);
        result = nn.fs.FileSystem.Commit(mountName);
        result.abortUnlessSuccess();

        UnityEngine.Switch.Notification.LeaveExitRequestHandlingSection();
        Console.WriteLine("FsSaveDataPlayerPrefs.InitializeSaveData(): Initialization complete.");
    }

    public void SavePlayerPrefs()
    {
        // CRITICAL: Force Unity to write current PlayerPrefs state into the helper rawData array first
        PlayerPrefs.Save();

        byte[] data = UnityEngine.Switch.PlayerPrefsHelper.rawData;
        Console.WriteLine("SavePlayerPrefs() size " + data.LongLength);
        long saveDataSize = data.LongLength;

        UnityEngine.Switch.Notification.EnterExitRequestHandlingSection();

        fileHandle = new nn.fs.FileHandle();
        nn.Result result = nn.fs.File.Open(ref fileHandle, filePath, nn.fs.OpenFileMode.Write);
        result.abortUnlessSuccess();

        result = nn.fs.File.SetSize(fileHandle, saveDataSize);
        result.abortUnlessSuccess();

        result = nn.fs.File.Write(fileHandle, 0, data, saveDataSize, nn.fs.WriteOption.Flush);
        result.abortUnlessSuccess();

        nn.fs.File.Close(fileHandle);
        result = nn.fs.FileSystem.Commit(mountName);
        result.abortUnlessSuccess();

        UnityEngine.Switch.Notification.LeaveExitRequestHandlingSection();
        Console.WriteLine("SavePlayerPrefs() finished saving to " + filePath);
    }

    private void Load()
    {
        Console.WriteLine("FsSaveDataPlayerPrefs.Load(): Loading save file");
        nn.fs.EntryType entryType = 0;
        nn.Result result = nn.fs.FileSystem.GetEntryType(ref entryType, filePath);
        if (nn.fs.FileSystem.ResultPathNotFound.Includes(result))
        {
            Console.WriteLine("FsSaveDataPlayerPrefs.Load(" + filePath + "): ERROR, path not found");
            return;
        }
        result.abortUnlessSuccess();

        result = nn.fs.File.Open(ref fileHandle, filePath, nn.fs.OpenFileMode.Read);
        result.abortUnlessSuccess();

        long fileSize = 0;
        result = nn.fs.File.GetSize(ref fileSize, fileHandle);
        result.abortUnlessSuccess();

        byte[] data = new byte[fileSize];
        result = nn.fs.File.Read(fileHandle, 0, data, fileSize);
        result.abortUnlessSuccess();
        nn.fs.File.Close(fileHandle);

        // Inject directly back into Unity's PlayerPrefs helper
        UnityEngine.Switch.PlayerPrefsHelper.rawData = data;

        // Force Unity to rebuild its internal dictionary from the newly injected rawData bytes
        int version = PlayerPrefs.GetInt(versionKey, 0);
        Console.WriteLine("FsSaveDataPlayerPrefs.Load(): loading complete, version = " + version);
    }
#endif
}