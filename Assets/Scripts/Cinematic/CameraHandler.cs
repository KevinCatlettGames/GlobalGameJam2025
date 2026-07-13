using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events; 

public class CameraHandler : NetworkBehaviour
{
    public static CameraHandler Instance;
    [SerializeField] private GameObject cinematicCamera;
    [SerializeField] private GameObject mainCamera;
    public bool playCinematicAtStart = true;
    public UnityEvent onCinematicEnd;
    public UnityEvent onTransitionHolding;
    bool onTransitionHoldingInvoked = false;

    private void Awake()
    {
        if(Instance == null) 
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        mainCamera.SetActive(false);
        cinematicCamera.SetActive(false);
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay)
        {
            if(IsServer) 
                LobbyManager.instance.OnAllPlayersLoadedIn.AddListener(BeginClientRpc);
        }
        else
        {
           Begin();
        }
    }

    void Begin()
    { 
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && IsServer)
        {
            LobbyManager.instance.OnAllPlayersLoadedIn.RemoveListener(Begin);
        }

        if (cinematicCamera == null || !playCinematicAtStart)
        {
            mainCamera.SetActive(true);
            Invoke(nameof(StartWithoutCinematic), 2f);
        }
        else
        {
            cinematicCamera.SetActive(true);
        }
    }

    [ClientRpc]
    void BeginClientRpc()
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && IsServer)
        {
            LobbyManager.instance.OnAllPlayersLoadedIn.RemoveListener(BeginClientRpc);
        }

        if (cinematicCamera == null || !playCinematicAtStart)
        {
            Invoke(nameof(StartWithoutCinematic), 2f);
        }
        else
        {
            cinematicCamera.SetActive(true);
        }
    }

    public void InvokeCinematicEnd()
    {
        cinematicCamera.SetActive(false);
        mainCamera.SetActive(true);
        onCinematicEnd?.Invoke();
    }

    void StartWithoutCinematic()
    {
        PlayerManager.Instance.StartPlayerJoining();
        onCinematicEnd?.Invoke();
    }

    public void OnTransitionHolding()
    {
        if (onTransitionHoldingInvoked) return;
        onTransitionHolding?.Invoke();
        onTransitionHoldingInvoked = true; 
    }
}