using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events; 

public class CameraHandler : NetworkBehaviour
{
    public static CameraHandler Instance;
    [SerializeField] private GameObject cinematicCamera;
    [SerializeField] private GameObject mainCamera;
    [SerializeField] Countdown countDown; 
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
        Invoke(nameof(Init), 0);
    }

    private void Init()
    {
        cinematicCamera.SetActive(false);
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay)
        {
            if (IsServer)
                LobbyManager.instance.OnAllPlayersLoadedIn.AddListener(BeginClientRpc);
        }
        else
        {
            Begin();
        }
    }

    void Begin()
    { 
        if (cinematicCamera == null || !playCinematicAtStart)
        {
            mainCamera.SetActive(true);
            cinematicCamera.SetActive(false);
            Invoke(nameof(StartWithoutCinematic), 2f);
        }
        else
        {
            mainCamera.SetActive(false);
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
            if (MenuTransitionHandler.Instance && MenuTransitionHandler.Instance.fadeIsOn)
                StartCoroutine(MenuTransitionHandler.Instance.PlayFadeAfterSceneChangeSmoothly());
            Invoke(nameof(StartWithoutCinematicOnline), 2f);
        }
        else
        {
            if (MenuTransitionHandler.Instance && MenuTransitionHandler.Instance.fadeIsOn)
                StartCoroutine(MenuTransitionHandler.Instance.PlayFadeAfterSceneChangeSmoothly());
            mainCamera.SetActive(false);
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
        mainCamera.SetActive(true);
        PlayerManager.Instance.StartPlayerJoining();
        onCinematicEnd?.Invoke();
    }

    void StartWithoutCinematicOnline()
    {
        mainCamera.SetActive(true);
        PlayerManager.Instance.StartPlayerJoining();
        onCinematicEnd?.Invoke();
        countDown?.OnCountdownStart?.Invoke();
        Invoke(nameof(EnablePlayersWithoutCinematicOnline), 1f);
    }


    void EnablePlayersWithoutCinematicOnline()
    {
        EnablePlayersWithoutCinematicClientRpc();
    }

    [ClientRpc]
    void EnablePlayersWithoutCinematicClientRpc()
    {
        List<PlayerController> players = PlayerManager.Instance.GetPlayers();
        foreach (PlayerController player in players)
            player.StartEntrence(true);

        countDown?.onCountdownComplete?.Invoke();
    }

    public void OnTransitionHolding()
    {
        if (onTransitionHoldingInvoked) return;
        onTransitionHolding?.Invoke();
        onTransitionHoldingInvoked = true; 
    }
}