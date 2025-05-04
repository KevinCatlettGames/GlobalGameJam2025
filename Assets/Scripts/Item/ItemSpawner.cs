using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class ItemSpawner : NetworkBehaviour
{
    public static ItemSpawner Instance;

    [SerializeField] GameObject itemPrefab;
    [SerializeField] Transform[] startSpawnPoints;
    [SerializeField] float minSpawnInterval = 1;
    [SerializeField] float maxSpawnInterval = 7;
    [SerializeField] float spawnRadius = 15;

    public int maxAmount = 2;
    public int currentAmount;

    private List<GameObject> spawnedItems = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            GameManager.Instance.OnGameStarted += ResetSpawner;

            foreach (Transform t in startSpawnPoints)
                SpawnItem(t.position);

            Invoke(nameof(SpawnLoop), Random.Range(minSpawnInterval, maxSpawnInterval));
        }
    }

    private void SpawnLoop()
    {
        if (!IsServer) return;

        if (currentAmount < maxAmount)
            SpawnItem(Vector3.zero);

        Invoke(nameof(SpawnLoop), Random.Range(minSpawnInterval, maxSpawnInterval));
    }

    private void SpawnItem(Vector3 location)
    {
        Vector3 spawnPosition;
        if (location == Vector3.zero)
        {
            Vector3 randomPos = Random.insideUnitSphere * spawnRadius;
            randomPos.y = itemPrefab.transform.position.y;
            spawnPosition = new Vector3(randomPos.x, itemPrefab.transform.position.y, randomPos.z);
        }
        else
        {
            spawnPosition = new Vector3(location.x, itemPrefab.transform.position.y, location.z);
        }

        GameObject itemInstance = Instantiate(itemPrefab, spawnPosition, Quaternion.identity);
        
        // Important: the prefab must have a NetworkObject component
        itemInstance.GetComponent<NetworkObject>().Spawn(true);

        spawnedItems.Add(itemInstance);
        currentAmount++;
    }

    public void ChangeMaxItemAmount(bool increase)
    {
        if (!IsServer) return;

        if (increase)
            maxAmount++;
        else
            maxAmount--;
    }

    public void ResetSpawner()
    {
        if (!IsServer) return;

        foreach (GameObject item in spawnedItems)
        {
            if (item != null)
                Destroy(item);
        }

        spawnedItems.Clear();
        currentAmount = 0;

        foreach (Transform t in startSpawnPoints)
            SpawnItem(t.position);

        Invoke(nameof(SpawnLoop), Random.Range(minSpawnInterval, maxSpawnInterval));
    }
}
