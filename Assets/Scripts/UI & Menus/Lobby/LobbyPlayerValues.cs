using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Unity.Netcode;
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
            TeamIndex = -1;
        }
    }

    public List<PlayerValues> playerValuesList = new();
    public int maxPlayers = 4;
    public int maxTeamSize = 2;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += SceneManagerOnsceneLoaded;

        if (IsServer && TransportSwitcher.Instance.isUsingRelay)
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
    }

    private void SceneManagerOnsceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == 0)
            Destroy(gameObject);

        if (!TransportSwitcher.Instance.isUsingRelay)
        {
            List<PlayerValues> playersToRemove = new List<PlayerValues>();


            foreach (PlayerValues playerValues in playerValuesList)
                if (playerValues.Device == null)
                    playersToRemove.Add(playerValues);

            foreach (PlayerValues p in playersToRemove)
                playerValuesList.Remove(p);

            playersToRemove.Clear();
        }
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        if (clientId == 0 ||!IsServer) return;

        GameObject player = Instantiate(lobbyPlayer);
        player.GetComponent<NetworkObject>().SpawnAsPlayerObject(clientId, true);

        if (IsServer)
            SharePlayerValues(clientId);
    }

    private void OnClientDisconnectedCallback(ulong clientId)
    {
        PlayerValues disconnectedPlayerValues = null; 
        foreach(PlayerValues values in  playerValuesList)
        {
            if(values.PlayerIndex == (int)clientId)
                disconnectedPlayerValues = values;
        }
        Debug.Log("Removal");
        if(disconnectedPlayerValues != null)
            playerValuesList.Remove(disconnectedPlayerValues);
    }

    private void SharePlayerValues(ulong clientIDToShareTo)
    {
        foreach (PlayerValues pv in playerValuesList)
        {
            bool isReady = LobbyManager.instance.playerContainers[pv.PlayerIndex]
                .GetComponent<PlayerContainerManager>()
                .isReady;

            SharePlayerValuesServerRpc(pv.PlayerIndex, pv.Skin.Index, isReady, clientIDToShareTo, pv.TeamIndex);
        }
    }

    [ServerRpc(RequireOwnership = true)]
    public void SharePlayerValuesServerRpc(int playerIndex, int skinIndex, bool isReady, ulong clientIDToShareTo, int teamIndex)
    {
        SharePlayerValuesClientRpc(playerIndex, skinIndex, isReady, clientIDToShareTo, teamIndex);
    }

    [ClientRpc]
    private void SharePlayerValuesClientRpc(int playerIndex, int skinIndex, bool isReady, ulong clientIDToShareTo, int teamIndex)
    {
        if (IsServer || NetworkManager.Singleton.LocalClientId != clientIDToShareTo)
            return;

        SkinSO skinToUse = LobbyManager.instance.PossibleSkins[0];

        foreach (SkinSO skin in LobbyManager.instance.PossibleSkins)
        {
            if (skin.Index == skinIndex)
            {
                skinToUse = skin;
                break;
            }
        }

        playerValuesList.Add(new PlayerValues(playerIndex, null, skinToUse, teamIndex));

        SortPlayerValues();
        LobbyManager.instance.UpdatePlayerUIAndOccupiedState();

        var skinChange = LobbyManager.instance.playerContainers[playerIndex]
            .GetComponent<PlayerContainerSkinChange>();

        skinChange.currentColorIndex = skinToUse.Index;
        skinChange.UpdateSkinServerRpc();

        if (isReady &&
            !LobbyManager.instance.playerContainers[playerIndex]
                .GetComponent<PlayerContainerManager>()
                .isReady)
        {
            LobbyManager.instance.playerContainers[playerIndex]
                .GetComponent<PlayerContainerManager>()
                .ReadyStateUpdated((ulong)playerIndex, false);
        }
    }

    [ServerRpc]
    public void AddNewPlayerValueServerRpc(int playerIndex, int skinIndex, bool isReady)
    {
        AddNewPlayerValueClientRpc(playerIndex, skinIndex, isReady);
    }

    [ClientRpc]
    public void AddNewPlayerValueClientRpc(int playerIndex, int skinIndex, bool isReady)
    {
        SkinSO skinToUse = LobbyManager.instance.PossibleSkins[0];

        foreach (SkinSO skin in LobbyManager.instance.PossibleSkins)
        {
            if (skin.Index == skinIndex)
            {
                skinToUse = skin;
                break;
            }
        }

        var existingPlayer = playerValuesList.Find(pd => pd.PlayerIndex == playerIndex);

        if (existingPlayer == null)
        {
            playerValuesList.Add(new PlayerValues(playerIndex, null, skinToUse, -1));

            SortPlayerValues();
        }
    }

    [ServerRpc]
    public void RemovePlayerValueServerRpc(int playerIndex)
    {
        RemovePlayerValueClientRpc(playerIndex);
    }

    [ClientRpc]
    public void RemovePlayerValueClientRpc(int playerIndex)
    {
        var existingPlayer = playerValuesList.Find(pd => pd.PlayerIndex == playerIndex);

        if (existingPlayer != null)
        {
            playerValuesList.Remove(existingPlayer);
        }

        SortPlayerValues();
    }

    public void SortPlayerValues()
    {
        playerValuesList.Sort((a, b) => a.PlayerIndex.CompareTo(b.PlayerIndex));
    }

    public void AssignDeviceToPlayer(int playerIndex, InputDevice device)
    {
        if (playerIndex < 0 || playerIndex >= maxPlayers)
            return;

        if (device == null)
            return;

        var existingPlayer = playerValuesList.Find(pd => pd.PlayerIndex == playerIndex);

        if (existingPlayer != null)
        {
            existingPlayer.Device = device;
        }
        else
        {
            playerValuesList.Add(
                new PlayerValues(
                    playerIndex,
                    device,
                    LobbyManager.instance.PossibleSkins[0],
                    -1
                )
            );

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
        if (device == null)
            return -1;

        int existingIndex = GetPlayerIndex(device);
        if (existingIndex != -1)
            return existingIndex;

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