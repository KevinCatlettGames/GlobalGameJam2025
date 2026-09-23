using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PingIndicator : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private Image pingIcon;
    [SerializeField] private TextMeshProUGUI pingText;
    [Header("Polling Rate")]
    [SerializeField] private float checkInterval = 1f;

    [Header("Ping Thresholds (ms)")]
    [SerializeField] private float moderatePing = 100f;
    [SerializeField] private float poorPing = 180f;
    [SerializeField] private float criticalPing = 300f;

    [Header("Colors")]
    [SerializeField] private Color moderateColor = Color.yellow;
    [SerializeField] private Color poorColor = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color criticalColor = Color.red;

    private float timer;

    private void Awake()
    {
        if(!TransportSwitcher.Instance || !TransportSwitcher.Instance.isUsingRelay)
        {
            pingIcon.enabled = false;
            this.enabled = false;
            pingText.enabled = false;
        }    
    }

    private void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
            return;

        timer += Time.deltaTime;
        if (timer >= checkInterval)
        {
            timer = 0f;
            UpdatePingStatus();
        }
    }

    private void UpdatePingStatus()
    {
        float pingMs = NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.ServerClientId);

        if (pingMs < moderatePing)
        {
            if (pingIcon.enabled)
            {
                pingIcon.enabled = false;
                pingText.enabled = false;
            }
        }
        else
        {
            if (!pingIcon.enabled)
            {
                pingIcon.enabled = true;
                pingText.enabled = true;
            }

            if (pingMs >= criticalPing)
                pingIcon.color = criticalColor;
            else if (pingMs >= poorPing)
                pingIcon.color = poorColor;
            else
                pingIcon.color = moderateColor;

            pingText.text = pingMs.ToString();
        }
    }
}