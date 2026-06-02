using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class LobbyPlayerValues : NetworkBehaviour
{
    public static LobbyPlayerValues Instance;

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

#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && !UNITY_SWITCH

        if (IsServer && TransportSwitcher.Instance.isUsingRelay)
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnectedCallback;
#endif
    }

    private void OnDisable()
    {
        playerValuesList.Clear();
    }

    private void OnDestroy()
    {
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && !UNITY_SWITCH
        SceneManager.sceneLoaded -= SceneManagerOnsceneLoaded;
#endif
    }

    private void SceneManagerOnsceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == 0)
            Destroy(gameObject);

        List<PlayerValues> playersToRemove = new List<PlayerValues>();


        foreach(PlayerValues playerValues in playerValuesList)
            if(playerValues.Device == null)
                playersToRemove.Add(playerValues);

        foreach (PlayerValues p in playersToRemove)
            playerValuesList.Remove(p);

        playersToRemove.Clear();
    }

    private void OnClientConnectedCallback(ulong clientId)
    {
        Invoke(nameof(SharePlayerValues), 0.5f);
    }

    private void SharePlayerValues()
    {
        foreach (PlayerValues pv in playerValuesList)
        {
            bool isReady = LobbyManager.instance.playerContainers[pv.PlayerIndex]
                .GetComponent<PlayerContainerManager>()
                .isReady;

            SharePlayerValuesServerRpc(pv.PlayerIndex, pv.Skin.Index, isReady);
        }
    }

    [ServerRpc(RequireOwnership = true)]
    private void SharePlayerValuesServerRpc(int playerIndex, int skinIndex, bool isReady)
    {
        SharePlayerValuesClientRpc(playerIndex, skinIndex, isReady);
    }

    [ClientRpc]
    private void SharePlayerValuesClientRpc(int playerIndex, int skinIndex, bool isReady)
    {
        if (IsServer)
            return;

        playerValuesList.Clear();

        SkinSO skinToUse = LobbyManager.instance.PossibleSkins[0];

        foreach (SkinSO skin in LobbyManager.instance.PossibleSkins)
        {
            if (skin.Index == skinIndex)
            {
                skinToUse = skin;
                break;
            }
        }

        playerValuesList.Add(new PlayerValues(playerIndex, null, skinToUse, -1));

        SortPlayerValues();
        LobbyManager.instance.UpdatePlayerUI();

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