using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ItemSpawner : MonoBehaviour
{
    public static ItemSpawner Instance;
    
    [SerializeField] GameObject itemPrefab;
    [SerializeField] Transform[] startSpawnPoints;
    [SerializeField] float spawnInterval;
    [SerializeField] private Vector2 xSpawnRange;
    [SerializeField] private Vector2 zSpawnRange;
    
    [SerializeField] List<GameObject> spawnedItems = new List<GameObject>();
    
    public int maxAmount; 
    public int currentAmount;

    private void Awake()
    {
        Instance = this; 
    }

    public void Start()
    {
        PlayerManager.Instance.OnPlayerWon += Reset;
        
        
        foreach (Transform t in startSpawnPoints)
            SpawnItem(t.position);

        Invoke(nameof(SpawnLoop), spawnInterval);
    }

    void SpawnLoop()
    {
        if (currentAmount < maxAmount)
            SpawnItem(Vector3.zero);

        Invoke(nameof(SpawnLoop), spawnInterval);
    }

    void SpawnItem(Vector3 location)
    {
        GameObject newItem; 
        
        if(location == Vector3.zero)
            newItem = Instantiate(itemPrefab, new Vector3(Random.Range(xSpawnRange.x, xSpawnRange.y), itemPrefab.transform.position.y, Random.Range(zSpawnRange.x, zSpawnRange.y)), Quaternion.identity);
        else
            newItem = Instantiate(itemPrefab, new Vector3(location.x, itemPrefab.transform.position.y, location.z), Quaternion.identity);
        
       currentAmount++;
       spawnedItems.Add(newItem);
    }

    public void Reset()
    {
        foreach (GameObject item in spawnedItems)
        { 
            Destroy(item);
            currentAmount = 0; 
        }
        
        foreach (Transform t in startSpawnPoints)
            SpawnItem(t.position);

        Invoke(nameof(SpawnLoop), spawnInterval);
    }
}