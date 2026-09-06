using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class LobbyPlayerValues : NetworkBehaviour
{
    public static LobbyPlayerValues Instance;
    public GameObject lobbyPlayer;

    [System.Serializable]
    public class PlayerValues
    {
        public int PlayerIndex;
        public InputDevice Device;
        public SkinSO Skin;
        public int TeamIndex;

        public PlayerValues(int playerIndex, InputDevice device, SkinSO skin, int teamIndex)
        {
            PlayerIndex = playerIndex;
            Device = device;
            Skin = skin;
            TeamIndex = teamIndex;
        }
    }

    public List<PlayerValues> playerValuesList = new();
    public int maxPlayers = 4;
    public int maxTeamSize = 2;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SceneManager.sceneLoaded += SceneManagerOnsceneLoaded;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnectedCallback;
        }
    }

    private void OnDisable()
    {
        playerValuesList.Clear();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= SceneManagerOnsceneLoaded;

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnectedCallback;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnectedCallback;
        }
    }

    private void SceneManagerOnsceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == 0)
        {
            Destroy(gameObject);
            return;
        }

        if (TransportSwitcher.Instance != null && !TransportSwitcher.Instance.isUsingRelay)
        {
            playerValuesList.RemoveAll(p => p.Device == null);
        }
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        if (!IsServer) return;

        if (lobbyPlayer != null)
        {
            GameObject player = Instantiate(lobbyPlayer);
            player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);
        }

        SharePlayerValuesToClient(clientId);
    }

    private void OnClientDisconnectedCallback(ulong clientId)
    {
        if (!IsServer) return;

        RemovePlayerValueServerRpc((int)clientId);
    }

    private void SharePlayerValuesToClient(ulong clientIDToShareTo)
    {
        foreach (PlayerValues pv in playerValuesList)
        {
            bool isReady = false;
            if (LobbyManager.instance != null && LobbyManager.instance.playerContainers.Length > pv.PlayerIndex)
            {
                var manager = LobbyManager.instance.playerContainers[pv.PlayerIndex].GetComponent<PlayerContainerManager>();
                if (manager != null) isReady = manager.isReady;
            }

            SyncSinglePlayerClientRpc(pv.PlayerIndex, isReady, clientIDToShareTo, pv.TeamIndex);
        }
    }

    [ClientRpc]
    private void SyncSinglePlayerClientRpc(int playerIndex, bool isReady, ulong targetClientId, int teamIndex)
    {
        if (NetworkManager.Singleton.LocalClientId != targetClientId) return;

        SkinSO defaultSkin = (LobbyManager.instance != null && LobbyManager.instance.PossibleSkins.Length > 0)
            ? LobbyManager.instance.PossibleSkins[0]
            : null;

        var existing = playerValuesList.Find(p => p.PlayerIndex == playerIndex);
        if (existing == null)
        {
            playerValuesList.Add(new PlayerValues(playerIndex, null, defaultSkin, teamIndex));
        }

        SortPlayerValues();

        if (LobbyManager.instance != null)
        {
            LobbyManager.instance.UpdatePlayerUIAndOccupiedState();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddNewPlayerValueServerRpc(int playerIndex, int skinIndex, bool isReady)
    {
        AddNewPlayerValueClientRpc(playerIndex, skinIndex, isReady);
    }


    [ClientRpc]
    public void AddNewPlayerValueClientRpc(int playerIndex, int skinIndex, bool isReady)
    {
        SkinSO skinToUse = null;
        if (LobbyManager.instance != null && LobbyManager.instance.PossibleSkins.Length > 0)
        {
            skinToUse = System.Array.Find(LobbyManager.instance.PossibleSkins, s => s.Index == skinIndex)
                        ?? LobbyManager.instance.PossibleSkins[0];
        }

        var existingPlayer = playerValuesList.Find(pd => pd.PlayerIndex == playerIndex);
        if (existingPlayer == null)
        {
            playerValuesList.Add(new PlayerValues(playerIndex, null, skinToUse, -1));
            SortPlayerValues();
        }

        if (LobbyManager.instance != null)
        {
            LobbyManager.instance.UpdatePlayerUIAndOccupiedState();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void RemovePlayerValueServerRpc(int playerIndex)
    {
        RemovePlayerValueClientRpc(playerIndex);
    }

    [ClientRpc]
    public void RemovePlayerValueClientRpc(int playerIndex)
    {
        playerValuesList.RemoveAll(pd => pd.PlayerIndex == playerIndex);
        SortPlayerValues();

        if (LobbyManager.instance != null)
        {
            LobbyManager.instance.UpdatePlayerUIAndOccupiedState();
        }
    }

    public void SortPlayerValues()
    {
        playerValuesList.Sort((a, b) => a.PlayerIndex.CompareTo(b.PlayerIndex));
    }

    public void AssignDeviceToPlayer(int playerIndex, InputDevice device)
    {
        if (playerIndex < 0 || playerIndex >= maxPlayers || device == null)
            return;

        var existingPlayer = playerValuesList.Find(pd => pd.PlayerIndex == playerIndex);
        if (existingPlayer != null)
        {
            existingPlayer.Device = device;
        }
        else
        {
            SkinSO defaultSkin = (LobbyManager.instance != null && LobbyManager.instance.PossibleSkins.Length > 0)
                ? LobbyManager.instance.PossibleSkins[0]
                : null;

            playerValuesList.Add(new PlayerValues(playerIndex, device, defaultSkin, -1));
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
        var pd = playerValuesList.Find(p => p.PlayerIndex == playerIndex);
        return pd?.Device;
    }

    public void RemoveDevice(int playerIndex)
    {
        playerValuesList.RemoveAll(p => p.PlayerIndex == playerIndex);
        SortPlayerValues();
    }
}