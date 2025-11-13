using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class WallManager : MonoBehaviour
{
    public static WallManager Instance;
    private List<RisingWall> walls;
    [SerializeField] private bool isMapEventActive = true;
    [SerializeField] private float stayTime = 5f;
    [SerializeField] private float sinkTime = 5f;
    [SerializeField] private int maxActiveWalls = 3;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float startDelay = 1f;
    private float timer = 90f;
    private bool initialised = false;
    private bool wallsActive = false;
    private int totalWalls = 0;
    private int[] wallIndex;
    private bool isMoving = true;
    private void Awake()
    {
        if (LobbyManager.instance)
            isMapEventActive = LobbyManager.instance.playWithMapEvents;
        
        if (!isMapEventActive)
        {
            Destroy(gameObject);
            return;
        } 
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
        walls = new List<RisingWall>();
        if (TransportSwitcher.Instance)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            GameManager.Instance.OnGameStarted += StartMoving;
            GameManager.Instance.OnGameEnded += StopMoving;
            Invoke(nameof(StartMoving), 7);
        }
        else
        {
            GameManager.Instance.OnGameStarted += StartMoving;
            GameManager.Instance.OnGameEnded += StopMoving;
            Invoke(nameof(StartMoving), 7);
        }
    }
    void Update()
    {
        if (!isMoving) return;
        if (timer <= 0)
        {
            if (!initialised)
            {
                totalWalls = walls.Count;
                wallIndex = new int[totalWalls];
                for (int i = 0; i < wallIndex.Length; i++)
                {
                    wallIndex[i] = i;   
                }
                initialised = true;
            }
            if (!wallsActive)
            {
                RiseWalls();
                timer = stayTime;
            }
            else
            {
                SinkWalls();
                timer = sinkTime;
            }
        }
        else
        {
            timer -= Time.deltaTime;
        }
        if (!wallsActive)
        {
            transform.Rotate(Vector3.up * (rotationSpeed * Time.deltaTime));
        }

        if (Input.GetKeyDown(KeyCode.B))
        {
            foreach (var Wall in walls)
            {
                Wall.Rise();
            }
        }
        else if (Input.GetKeyDown(KeyCode.N))
        {
            foreach (var Wall in walls)
            {
                Wall.Sink();
            }
        }
    }
    private void RiseWalls()
    {
        ShuffleIndexes();
        for (int i = 0; i < maxActiveWalls; i++)
        {
            walls[wallIndex[i]].Rise();
        }
        wallsActive = true;
    }
    private void SinkWalls()
    {
        for (int i = 0; i < maxActiveWalls; i++)
        {
            walls[wallIndex[i]].Sink();
        }
        Invoke(nameof(SetWallsInactive), 1.5f);
    }
    private void SetWallsInactive()
    {
        wallsActive = false;
    }
    private void StartMoving()
    {
        timer = sinkTime + startDelay;
        isMoving = true;
    }
    private void StopMoving()
    {
        SinkWalls();
        isMoving = false;
    }
    public void AddWall(RisingWall wall)
    {
        walls.Add(wall);
    }
    private void ShuffleIndexes()
    {
        for (int i = wallIndex.Length -1; i > 0; i--)
        {
            int x = Random.Range(0, i + 1);
            int temp = wallIndex[i];
            wallIndex[i] = wallIndex[x];
            wallIndex[x] = temp;
        }
    }
    private void OnDestroy()
    {
        if (TransportSwitcher.Instance)
        {
            if (NetworkManager.Singleton && !NetworkManager.Singleton.IsServer) return;
            GameManager.Instance.OnGameStarted -= StartMoving;
            GameManager.Instance.OnGameEnded -= StopMoving;
        }
        else
        {
            GameManager.Instance.OnGameStarted -= StartMoving;
            GameManager.Instance.OnGameEnded -= StopMoving;
        }
    }
}
