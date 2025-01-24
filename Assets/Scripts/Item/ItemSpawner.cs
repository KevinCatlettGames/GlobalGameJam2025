using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemSpawner : MonoBehaviour
{
    [SerializeField] GameObject itemPrefab;
    [SerializeField] Transform[] startSpawnPoints;
    [SerializeField] float spawnInterval;
    [SerializeField] private Vector2 xSpawnRange;
    [SerializeField] private Vector2 zSpawnRange;
    
    public void Start()
    {
        foreach (Transform t in startSpawnPoints)
            SpawnItem(t.position);

        Invoke(nameof(SpawnLoop), spawnInterval);
    }

    void SpawnLoop()
    {
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
        
       
    }
}