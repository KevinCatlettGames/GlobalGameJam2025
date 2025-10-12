using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class WallManager : MonoBehaviour
{
    public static WallManager Instance;
    private List<RisingWall> walls;
    [SerializeField] private bool isMapEventActive = true;
    [SerializeField] private float stayTime = 5f;
    [SerializeField] private int maxActiveWalls = 3;
    private float timer = 0f;
    private bool initialised = false;
    private bool wallsActive = false;
    private int totalWalls = 0;
    private int[] wallIndex;
    private void Awake()
    {
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
        timer = stayTime;
    }
    void Start()
    {
        
    }

    void Update()
    {
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
                ShuffleIndexes();
                for (int i = 0; i < maxActiveWalls; i++)
                {
                    walls[wallIndex[i]].Rise();
                }
                wallsActive = true;
            }
            else
            {
                for (int i = 0; i < maxActiveWalls; i++)
                {
                    walls[wallIndex[i]].Sink();
                }
                wallsActive = false;
            }
            timer = stayTime;
        }
        else
        {
            timer -= Time.deltaTime;
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
}
