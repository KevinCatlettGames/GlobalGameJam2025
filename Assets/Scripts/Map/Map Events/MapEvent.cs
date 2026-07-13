using Unity.Netcode;
using UnityEngine;

public abstract class MapEvent : NetworkBehaviour
{
    [SerializeField] private float firstStartDelay = 5f;
    [SerializeField] private int mapID = 0;
    public void InitialiseMapEvent()
    {
        bool isMapEventEnabled = true;
        if (LobbyManager.instance)
            isMapEventEnabled = LobbyManager.instance.MapSettings[mapID].PlayWithMapEvent;

        if (!isMapEventEnabled)
        {
            Destroy(gameObject);
            return;
        }

        if (TransportSwitcher.Instance)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            GameManager.Instance.OnGameStarted += StartEvent;
            GameManager.Instance.OnGameEnded += StopEvent;
            Invoke(nameof(StartEvent), firstStartDelay);
        }
        else
        {
            GameManager.Instance.OnGameStarted += StartEvent;
            GameManager.Instance.OnGameEnded += StopEvent;
            Invoke(nameof(StartEvent), firstStartDelay);
        }
    }

    protected abstract void StartEvent();
    protected abstract void StopEvent();

    private void OnDestroy()
    {
        if (TransportSwitcher.Instance)
        {
            if (NetworkManager.Singleton && !NetworkManager.Singleton.IsServer) return;
            GameManager.Instance.OnGameStarted -= StartEvent;
            GameManager.Instance.OnGameEnded -= StopEvent;
        }
        else
        {
            GameManager.Instance.OnGameStarted -= StartEvent;
            GameManager.Instance.OnGameEnded -= StopEvent;
        }
    }
}
