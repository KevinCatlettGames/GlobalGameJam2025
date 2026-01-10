using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class WallManager : MonoBehaviour
{
    [SerializeField] private List<WallFormation> wallFormations;
    [SerializeField] private float stayTime = 5f;
    [SerializeField] private float sinkTime = 5f;
    [SerializeField] private float startDelay = 3f;
    private int currentFormation = -1;
    private bool wallsActive = false;


    private void Awake()
    {
        bool isMapEventActive = true;
        if (LobbyManager.instance)
            isMapEventActive = LobbyManager.instance.playWithMapEvents;
        
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
    void Update()
    {
        // Remove in final build!
        if (Input.GetKeyDown(KeyCode.B))
        {
            if (wallsActive)
            {
                SinkWalls();
            }
            CancelInvoke();
            RiseWalls();
        }
        else if (Input.GetKeyDown(KeyCode.N))
        {
            CancelInvoke();
            SinkWalls();
        }
    }
    private void RiseWalls()
    {
        if (wallsActive)
            return;

        int r = Random.Range(0, wallFormations.Count);
        if (r == currentFormation)
        {
            r++;
            if(r >= wallFormations.Count) 
                r = 0;
        }
        wallFormations[r].RiseFormation();
        currentFormation = r;
        wallsActive = true;
        Invoke(nameof(SinkWalls), stayTime);
    }
    private void SinkWalls()
    {
        if (!wallsActive)
            return;

        wallFormations[currentFormation].SinkFormation();
        wallsActive = false;

        Invoke(nameof(RiseWalls), sinkTime);
    }

    private void StartEvent()
    {
        Invoke(nameof(RiseWalls), startDelay);
    }
    private void StopEvent()
    {
        if (wallsActive)
        {
            wallFormations[currentFormation].SinkFormation();
            wallsActive = false;
        }

        CancelInvoke();
        currentFormation = -1;
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
