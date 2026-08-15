using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PingDisplay : MonoBehaviour
{
    [Header("UI Reference")]
    [Tooltip("Assign a TextMeshProUGUI or standard Text component here.")]
    [SerializeField] private TextMeshProUGUI pingTextTMP;

    [Header("Settings")]
    [SerializeField] private float updateInterval = 0.5f; // Update twice a second to prevent text flicker

    private float timer;

    private void Update()
    {
        // Only run if Netcode is active and connected
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
        {
            SetText("Ping: N/A");
            return;
        }

        timer += Time.deltaTime;
        if (timer >= updateInterval)
        {
            timer = 0f;
            UpdatePing();
        }
    }

    private void UpdatePing()
    {
        // Host / Singleplayer check
        if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsServer && !NetworkManager.Singleton.IsClient)
        {
            SetText("Ping: 0 ms (Host)");
            return;
        }

        // Retrieve RTT (Round Trip Time) from the underlying transport layer
        ulong currentPingMs = NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.ServerClientId);

        SetText($"Ping: {currentPingMs} ms");
    }

    private void SetText(string text)
    {
        Debug.Log(text);
    }
}