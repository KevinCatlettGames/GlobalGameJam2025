using UnityEngine;

public class FsSaveDataPlayerPrefs : MonoBehaviour
{
    public static FsSaveDataPlayerPrefs Instance { get; private set; }

    private const string mountName = "MySave";
    private const string versionKey = "Version";
    private const string counterKey = "Counter";
    private const int saveDataVersion = 1;

#if UNITY_SWITCH && !UNITY_EDITOR
    private const string fileName = "MySaveData";
    private static readonly string filePath = string.Format("{0}:/{1}", mountName, fileName);
    private nn.fs.FileHandle fileHandle = new nn.fs.FileHandle();
    private nn.account.Uid userId;
#endif

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
#if UNITY_SWITCH && !UNITY_EDITOR
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
        LoadGameData();
#endif
    }

    private void OnDestroy()
    {
#if UNITY_SWITCH && !UNITY_EDITOR
        nn.fs.FileSystem.Unmount(mountName);
#endif
    }

    public void SaveGameData()
    {
#if UNITY_SWITCH && !UNITY_EDITOR
        byte[] data = UnityEngine.Switch.PlayerPrefsHelper.rawData;

        UnityEngine.Switch.Notification.EnterExitRequestHandlingSection();

        nn.Result result = nn.fs.File.Open(ref fileHandle, filePath, nn.fs.OpenFileMode.Write);
        result.abortUnlessSuccess();

        result = nn.fs.File.Write(fileHandle, 0, data, data.LongLength, nn.fs.WriteOption.Flush);
        result.abortUnlessSuccess();

        nn.fs.File.Close(fileHandle);

        result = nn.fs.FileSystem.Commit(mountName);
        result.abortUnlessSuccess();

        UnityEngine.Switch.Notification.LeaveExitRequestHandlingSection();
#endif
    }

    public void LoadGameData()
    {
#if UNITY_SWITCH && !UNITY_EDITOR
        nn.fs.EntryType entryType = 0;
        nn.Result result = nn.fs.FileSystem.GetEntryType(ref entryType, filePath);
        if (nn.fs.FileSystem.ResultPathNotFound.Includes(result)) { return; }
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

        UnityEngine.Switch.PlayerPrefsHelper.rawData = data;
#endif
    }

    private void InitializeSaveData()
    {
#if UNITY_SWITCH && !UNITY_EDITOR
        nn.fs.EntryType entryType = 0;
        nn.Result result = nn.fs.FileSystem.GetEntryType(ref entryType, filePath);
        if (result.IsSuccess())
        {
            return;
        }
        if (!nn.fs.FileSystem.ResultPathNotFound.Includes(result))
        {
            result.abortUnlessSuccess();
        }

        PlayerPrefs.SetInt(versionKey, saveDataVersion);
        PlayerPrefs.SetInt(counterKey, 0);

        byte[] data = UnityEngine.Switch.PlayerPrefsHelper.rawData;

        UnityEngine.Switch.Notification.EnterExitRequestHandlingSection();

        result = nn.fs.File.Create(filePath, data.LongLength);
        result.abortUnlessSuccess();

        result = nn.fs.File.Open(ref fileHandle, filePath, nn.fs.OpenFileMode.Write);
        result.abortUnlessSuccess();

        result = nn.fs.File.Write(fileHandle, 0, data, data.LongLength, nn.fs.WriteOption.Flush);
        result.abortUnlessSuccess();

        nn.fs.File.Close(fileHandle);

        result = nn.fs.FileSystem.Commit(mountName);
        result.abortUnlessSuccess();

        UnityEngine.Switch.Notification.LeaveExitRequestHandlingSection();
#endif
    }
}