using UnityEngine.InputSystem;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class LobbyPlayerHandler : NetworkBehaviour
{
    public static LobbyPlayerHandler Instance;
    [System.Serializable]
    public class PlayerValues
    {
        public PlayerValues(int playerIndex, InputDevice device, SkinSO skin, int teamIndex)
        {
            PlayerIndex = playerIndex;
            Device = device;
            Skin = skin;
            TeamIndex = teamIndex;
        }

        public int PlayerIndex;
        public InputDevice Device;
        public SkinSO Skin;
        public int TeamIndex = -1;
    }

    public List<PlayerValues> playerValuesList = new List<PlayerValues>();
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

        playerValuesList.Add(new PlayerValues(playerIndex, null, skinToUse, -1));
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
    
    public void SortPlayerValues()
    {
        playerValuesList.Sort((a, b) => a.PlayerIndex.CompareTo(b.PlayerIndex));
    }
    
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
            playerValuesList.Add(new PlayerValues(playerIndex, device, LobbyManager.instance.PossibleSkins[0], -1));
            SortPlayerValues();
        }
    }
    
    public int GetPlayerIndex(InputDevice device)
    {
        var pd = playerValuesList.Find(p => p.Device == device);
        return pd != null ? pd.PlayerIndex : -1;
    }
    
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
    
    public InputDevice GetDevice(int playerIndex)
    {
        for (int i = 0; i < playerValuesList.Count; i++)
        {
            if (playerValuesList[i].PlayerIndex == playerIndex)
                return playerValuesList[i].Device;
        }
        return null;
    }

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