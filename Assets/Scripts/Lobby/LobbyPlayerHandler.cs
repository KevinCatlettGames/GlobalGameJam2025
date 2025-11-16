using UnityEngine.InputSystem;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.SceneManagement;

/// <summary>
/// Handles player-device assignments and synchronization across clients in the lobby.
/// Maintains a list of player values including index, assigned device, and selected skin.
/// Ensures lobby UI reflects player states correctly.
/// </summary>
public class LobbyPlayerHandler : NetworkBehaviour
{
    /// <summary>
    /// Singleton instance of the LobbyPlayerHandler.
    /// </summary>
    public static LobbyPlayerHandler Instance;

    /// <summary>
    /// Stores data for each player including player index, assigned input device, and selected skin.
    /// </summary>
    [System.Serializable]
    public class PlayerValues
    {
        public PlayerValues(int playerIndex, InputDevice device, SkinSO skin)
        {
            PlayerIndex = playerIndex;
            Device = device;
            Skin = skin;
        }

        /// <summary>Logical player index (0,1,2,3).</summary>
        public int PlayerIndex;

        /// <summary>The input device assigned to this player.</summary>
        public InputDevice Device;

        /// <summary>The selected skin for this player.</summary>
        public SkinSO Skin;
    }

    /// <summary>List of all connected players and their values.</summary>
    public List<PlayerValues> playerValuesList = new List<PlayerValues>();

    /// <summary>Maximum number of players supported locally.</summary>
    public int maxPlayers = 4;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += SceneManagerOnsceneLoaded;

        if(IsServer && TransportSwitcher.Instance.isUsingRelay) 
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        Invoke(nameof(SharePlayerValues), .5f);
    }

    void SharePlayerValues()
    {
        foreach (PlayerValues pv in playerValuesList)
        {
            bool isReady = LobbyManager.instance.playerContainers[pv.PlayerIndex]
                .GetComponent<PlayerContainerManager>().isReady;
            SharePlayerValuesServerRpc(pv.PlayerIndex, pv.Skin.Index, isReady);
        }
    }

    [ServerRpc(RequireOwnership = true)]
    void SharePlayerValuesServerRpc(int playerIndex, int skinIndex, bool isReady)
    {
        SharePlayerValuesClientRpc(playerIndex, skinIndex, isReady);
    }

    [ClientRpc]
    void SharePlayerValuesClientRpc(int playerIndex, int skinIndex, bool isReady)
    {
        if (IsServer) return;

        playerValuesList.Clear();

        SkinSO skinToUse = LobbyManager.instance.PossibleSkins[0];
        foreach (SkinSO skin in LobbyManager.instance.PossibleSkins)
        {
            if(skin.Index == skinIndex)
                skinToUse = skin;
        }

        playerValuesList.Add(new PlayerValues(playerIndex, null, skinToUse));
        SortPlayerValues();
        LobbyManager.instance.UpdatePlayerUI();

        LobbyManager.instance.playerContainers[playerIndex]
            .GetComponent<PlayerContainerSkinChange>()
            .currentColorIndex = skinToUse.Index;

        LobbyManager.instance.playerContainers[playerIndex]
            .GetComponent<PlayerContainerSkinChange>()
            .UpdateSkinServerRpc();

        if(isReady && !LobbyManager.instance.playerContainers[playerIndex]
               .GetComponent<PlayerContainerManager>().isReady)
        {
            LobbyManager.instance.playerContainers[playerIndex]
                .GetComponent<PlayerContainerManager>()
                .ReadyStateUpdated((ulong)playerIndex);
        }
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= SceneManagerOnsceneLoaded;
    }

    private void SceneManagerOnsceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        if (arg0.buildIndex == 0)
            Destroy(gameObject);
    }

    /// <summary>
    /// Keeps the playerValues list sorted by PlayerIndex.
    /// </summary>
    public void SortPlayerValues()
    {
        playerValuesList.Sort((a, b) => a.PlayerIndex.CompareTo(b.PlayerIndex));
    }

    /// <summary>
    /// Assigns a device to a specific player index.
    /// </summary>
    public void AssignDeviceToPlayer(int playerIndex, InputDevice device)
    {
        if (playerIndex < 0 || playerIndex >= maxPlayers) return;
        if (device == null) return;
        
        var existingDevice = playerValuesList.Find(pd => pd.Device == device);
        if (existingDevice != null) return;

        var existingPlayer = playerValuesList.Find(pd => pd.PlayerIndex == playerIndex);
        if (existingPlayer != null)
        {
            existingPlayer.Device = device;
        }
        else
        {
            playerValuesList.Add(new PlayerValues(playerIndex, device, LobbyManager.instance.PossibleSkins[0]));
            SortPlayerValues();
        }
    }

    /// <summary>
    /// Returns the logical player index for a given input device.
    /// </summary>
    public int GetPlayerIndex(InputDevice device)
    {
        var pd = playerValuesList.Find(p => p.Device == device);
        return pd != null ? pd.PlayerIndex : -1;
    }

    /// <summary>
    /// Assigns a device automatically to the next free player slot.
    /// </summary>
    public int AssignDeviceToNextFreePlayer(InputDevice device)
    {
        if (device == null) return -1;

        int existingIndex = GetPlayerIndex(device);
        if (existingIndex != -1) return existingIndex;

        for (int i = 0; i < maxPlayers; i++)
        {
            if (!playerValuesList.Exists(pd => pd.PlayerIndex == i))
            {
                AssignDeviceToPlayer(i, device);
                return i;
            }
        }

        return -1;
    }

    /// <summary>
    /// Returns the device assigned to a specific player index.
    /// </summary>
    public InputDevice GetDevice(int playerIndex)
    {
        for (int i = 0; i < playerValuesList.Count; i++)
        {
            if (playerValuesList[i].PlayerIndex == playerIndex)
                return playerValuesList[i].Device;
        }
        return null;
    }

    /// <summary>
    /// Removes the device assignment from a specific player index.
    /// </summary>
    public void RemoveDevice(int playerIndex)
    {
        for (int i = 0; i < playerValuesList.Count; i++)
        {
            if (playerValuesList[i].PlayerIndex == playerIndex)
            {
                playerValuesList.RemoveAt(i);
                SortPlayerValues();
                break;
            }
        }
    }
}