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
    [SerializeField] private float minSpawnInterval = 1;
    [SerializeField] private float maxSpawnInterval = 7;
    [SerializeField] private float spawnRadius = 15;
    [SerializeField] List<GameObject> spawnedItems = new List<GameObject>();
    
    public int maxAmount = 2; 
    public int currentAmount;

    private void Awake()
    {
        Instance = this; 
    }

    public void Start()
    {
        GameManager.Instance.OnGameStarted += Reset;
        
        
        foreach (Transform t in startSpawnPoints)
            SpawnItem(t.position);

        Invoke(nameof(SpawnLoop), Random.Range(minSpawnInterval, maxSpawnInterval));
    }

    void SpawnLoop()
    {
        if (currentAmount < maxAmount)
            SpawnItem(Vector3.zero);

        Invoke(nameof(SpawnLoop), Random.Range(minSpawnInterval, maxSpawnInterval));
    }

    void SpawnItem(Vector3 location)
    {
        GameObject newItem; 
        Vector3 center = new Vector3(0, 1, 0); // Center of the sphere
        Vector3 randomPosition = center + Random.insideUnitSphere * spawnRadius;
        
        if(location == Vector3.zero)
            newItem = Instantiate(itemPrefab, new Vector3(randomPosition.x, itemPrefab.transform.position.y, randomPosition.y), Quaternion.identity);
        else
            newItem = Instantiate(itemPrefab, new Vector3(location.x, itemPrefab.transform.position.y, location.z), Quaternion.identity);
        
       currentAmount++;
       spawnedItems.Add(newItem);
    }

    public void ChangeMaxItemAmount(bool increase)
    {
        if (increase)
        {
            maxAmount++;
        }
        else
        {
            maxAmount--;
        }
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

        Invoke(nameof(SpawnLoop), Random.Range(minSpawnInterval, maxSpawnInterval));
    }
}