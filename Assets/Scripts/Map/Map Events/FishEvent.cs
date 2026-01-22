using Unity.Netcode;
using UnityEngine;


public class FishEvent : MonoBehaviour
{
    [SerializeField] private JumpingFish jumpingFish;
    [SerializeField] private Transform[] jumpingPoints;
    [SerializeField] private float startDelay = 3f;
    [SerializeField] private float jumpDelay = 10f;
    private bool isLeft = false;
    private int jp_offset = 0;

    private void Awake()
    {
        bool isMapEventActive = true;
        if (LobbyManager.instance)
            isMapEventActive = LobbyManager.instance.MapSettings[3].PlayWithMapEvent;

        if (!isMapEventActive)
        {
            Destroy(gameObject);
            return;
        }

        if (TransportSwitcher.Instance)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            GameManager.Instance.OnGameStarted += StartEvent;
            GameManager.Instance.OnGameEnded += StopEvent;
            Invoke(nameof(StartEvent), 7);
        }
        else
        {
            GameManager.Instance.OnGameStarted += StartEvent;
            GameManager.Instance.OnGameEnded += StopEvent;
            Invoke(nameof(StartEvent), 7);
        }
    }

    void Start()
    {
        jp_offset = jumpingPoints.Length / 2;
    }
    private void StartEvent()
    {
        Invoke(nameof(FishGo), startDelay);
    }
    private void StopEvent()
    {
        CancelInvoke();
    }
    private void FishGo()
    {
        int r = Random.Range(0, jp_offset);
        r = isLeft ? r : r + jp_offset;
        isLeft = !isLeft;
        jumpingFish.Jump(jumpingPoints[r]);
        Invoke(nameof(FishGo), jumpDelay);
    }
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
