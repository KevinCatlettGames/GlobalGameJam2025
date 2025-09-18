using System;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;

public class LobbyPlayerHandler : NetworkBehaviour
{
    public static LobbyPlayerHandler Instance;

    [System.Serializable]
    public class PlayerValues
    {
        public int PlayerIndex; // 0,1,2,3
        public InputDevice Device;
        public SkinSO Skin;
    }

    public List<PlayerValues> playerValues = new List<PlayerValues>();

    public int maxPlayers = 4;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay)
            maxPlayers = 1;
    }

    private void Start()
    {
        if(NetworkManager.Singleton.IsServer) 
            transform.parent = null; 
        
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += SceneManagerOnsceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= SceneManagerOnsceneLoaded;
    }

    private void SceneManagerOnsceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
       if(arg0.buildIndex == 0)
           Destroy(gameObject);
    }

    /// <summary>
    /// Assigns a device to a logical player index
    /// </summary>
    public void AssignDeviceToPlayer(int playerIndex, InputDevice device)
    {
        if (playerIndex < 0 || playerIndex >= maxPlayers) return;
        if (device == null) return;

        // Prevent duplicate assignment
        var existingDevice = playerValues.Find(pd => pd.Device == device);
        if (existingDevice != null) return;

        var existingPlayer = playerValues.Find(pd => pd.PlayerIndex == playerIndex);
        if (existingPlayer != null)
        {
            existingPlayer.Device = device; // reassign device to this player
        }
        else
        {
            
            playerValues.Add(new PlayerValues { PlayerIndex = playerIndex, Device = device});
        }

        //Debug.Log($"Assigned device {device.displayName} to player {playerIndex}");
    }

    /// <summary>
    /// Returns the logical player index for a given input device
    /// </summary>
    public int GetPlayerIndex(InputDevice device)
    {
        var pd = playerValues.Find(p => p.Device == device);
        return pd != null ? pd.PlayerIndex : -1;
    }

    /// <summary>
    /// Assign a device automatically to the next free player slot
    /// </summary>
    public int AssignDeviceToNextFreePlayer(InputDevice device)
    {
        if (device == null) return -1;

        // Check if the device is already assigned
        int existingIndex = GetPlayerIndex(device);
        if (existingIndex != -1) return existingIndex;

        // Find the next free slot
        for (int i = 0; i < maxPlayers; i++)
        {
            if (!playerValues.Exists(pd => pd.PlayerIndex == i))
            {
                AssignDeviceToPlayer(i, device);
                return i;
            }
        }

        //Debug.LogWarning("No free player slots available for device: " + device.displayName);
        return -1;
    }
    
    public InputDevice GetDevice(int playerIndex)
    {
        for (int i = 0; i < playerValues.Count; i++)
        {
            if (playerValues[i].PlayerIndex == playerIndex)
                return playerValues[i].Device;
        }
        return null;
    }
    
    public void RemoveDevice(int playerIndex)
    {
        for (int i = 0; i < playerValues.Count; i++)
        {
            if (playerValues[i].PlayerIndex == playerIndex)
            {
                playerValues.RemoveAt(i);
                break;
            }
        }
    }
}
