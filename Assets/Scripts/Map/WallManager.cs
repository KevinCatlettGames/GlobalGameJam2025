using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class WallManager : MonoBehaviour
{
    public static WallManager Instance;
    public List<RisingWall> InactiveWalls;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
        Cursor.visible = false;
        InactiveWalls = new List<RisingWall>();
    }
    void Start()
    {
        
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            foreach (var Wall in InactiveWalls)
            {
                Wall.Rise();
            }
        }
        else if (Input.GetKeyDown(KeyCode.N))
        {
            foreach (var Wall in InactiveWalls)
            {
                Wall.Sink();
            }
        }
    }
}
