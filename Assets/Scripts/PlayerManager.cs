using UnityEngine;

public class PlayerManager : MonoBehaviour
{
    public static PlayerManager Instance; 
    public Transform[] spawnPoints; // Array of spawn points
    private int spawnPointsUsed = -1;
    
    void Awake()
    {
        Instance = this; 
    }

    public Vector3 GetNonUsedStartPosition()
    {
        spawnPointsUsed++; 
        return spawnPoints[spawnPointsUsed].position;
    }
}
