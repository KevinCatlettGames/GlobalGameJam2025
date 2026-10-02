using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PingIndicator : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private Image pingIcon;
    [SerializeField] private TextMeshProUGUI pingText;

    [Header("Polling & Delays")]
    [SerializeField] private float checkInterval = 1f;
    [SerializeField] private float startupDelay = 10f;

    [Header("Ping Thresholds (ms)")]
    [SerializeField] private float moderatePing = 100f;
    [SerializeField] private float poorPing = 180f;
    [SerializeField] private float criticalPing = 300f;

    [Header("Smoothing & Hysteresis")]
    [Tooltip("Lower values (e.g. 0.2) smooth out sudden spikes; higher values (e.g. 0.8) react faster.")]
    [Range(0.05f, 1f)]
    [SerializeField] private float smoothingFactor = 0.25f;

    [Tooltip("Ping must stay below (moderatePing - hysteresisMargin) before the UI turns off.")]
    [SerializeField] private float hysteresisMargin = 15f;

    [Tooltip("How long (in seconds) ping must remain good before the UI actually hides.")]
    [SerializeField] private float hideDelay = 2f;

    [Header("Colors")]
    [SerializeField] private Color moderateColor = Color.yellow;
    [SerializeField] private Color poorColor = new Color(1f, 0.5f, 0f);
    [SerializeField] private Color criticalColor = Color.red;

    private float timer;
    private float delayTimer;
    private float smoothedPing = -1f;
    private float goodPingTimer;

    private void Awake()
    {
        if (!TransportSwitcher.Instance || !TransportSwitcher.Instance.isUsingRelay)
        {
            SetUIEnabled(false);
            this.enabled = false;
        }
    }

    private void Start()
    {
        SetUIEnabled(false);
    }

    private void Update()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsClient)
            return;

        if (delayTimer < startupDelay)
        {
            delayTimer += Time.deltaTime;
            return;
        }

        timer += Time.deltaTime;
        if (timer >= checkInterval)
        {
            timer = 0f;
            UpdatePingStatus();
        }
    }

    private void UpdatePingStatus()
    {
        float rawPingMs = NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(NetworkManager.ServerClientId);

        if (smoothedPing < 0f)
        {
            smoothedPing = rawPingMs;
        }
        else
        {
            smoothedPing = Mathf.Lerp(smoothedPing, rawPingMs, smoothingFactor);
        }

        bool isCurrentlyShowing = pingIcon != null && pingIcon.enabled;
        float offThreshold = moderatePing - hysteresisMargin;

        if (isCurrentlyShowing)
        {
            if (smoothedPing < offThreshold)
            {
                goodPingTimer += checkInterval;
                if (goodPingTimer >= hideDelay)
                {
                    SetUIEnabled(false);
                    goodPingTimer = 0f;
                }
            }
            else
            {
                goodPingTimer = 0f;
            }
        }
        else
        {
            if (smoothedPing >= moderatePing)
            {
                SetUIEnabled(true);
                goodPingTimer = 0f;
            }
        }

        if (pingIcon != null && pingIcon.enabled)
        {
            if (smoothedPing >= criticalPing)
                pingIcon.color = criticalColor;
            else if (smoothedPing >= poorPing)
                pingIcon.color = poorColor;
            else
                pingIcon.color = moderateColor;

            pingText.text = Mathf.RoundToInt(smoothedPing).ToString();
        }
    }

    private void SetUIEnabled(bool state)
    {
        if (pingIcon != null) pingIcon.enabled = state;
        if (pingText != null) pingText.enabled = state;
    }
}