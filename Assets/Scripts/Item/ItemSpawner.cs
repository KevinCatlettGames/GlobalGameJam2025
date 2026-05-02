using System;
using System.Collections.Generic;
using System.Drawing;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class ItemSpawner : MonoBehaviour
{
    public static ItemSpawner Instance;

    [SerializeField] GameObject itemPrefab;
    [SerializeField] Transform[] startSpawnPoints;
    [SerializeField] float minSpawnInterval = 1;
    [SerializeField] float maxSpawnInterval = 7;
    [SerializeField] float spawnRadius = 15;
    [SerializeField] private float side = 0f;
    [SerializeField] SO_Spell[] spawnableItems;
    [SerializeField] bool spawningEnabled = true;
    public SO_Spell[]  SpawnableItems { get { return spawnableItems; } }
    
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

    public void InitialSpawn()
    {
        if (NetworkManager.Singleton.IsServer && spawningEnabled)
        {
            GameManager.Instance.OnGameStarted += ResetSpawner;

            foreach (Transform t in startSpawnPoints)
                SpawnItem(t.position);

            Invoke(nameof(SpawnLoop), Random.Range(minSpawnInterval, maxSpawnInterval));
        }
    }

    private void SpawnLoop()
    {
        if (NetworkManager.Singleton && !NetworkManager.Singleton.IsServer) return;

        if (currentAmount < maxAmount)
            SpawnItem(Vector3.zero);

        Invoke(nameof(SpawnLoop), Random.Range(minSpawnInterval, maxSpawnInterval));
    }

    private void SpawnItem(Vector3 location)
    {
        if(!spawningEnabled) return;
        Vector3 spawnPosition;
        if (location == Vector3.zero)
        {
            Vector3 randomPos;
            int i = 0;
            do
            {
                randomPos = Random.insideUnitSphere * spawnRadius;
                if (side != 0)
                {
                    randomPos.x += Random.Range(-side, side);
                }
                randomPos.y = itemPrefab.transform.position.y;
                Collider[] wallOverlaps = Physics.OverlapSphere(randomPos, 2.3f, LayerMask.GetMask("Wall"));
                if (wallOverlaps.Length == 0) break;
                i++;
            } while (i < 10);

            if (i == 10) return;
            spawnPosition = randomPos;
        }
        else
        {
            spawnPosition = new Vector3(
                location.x, itemPrefab.transform.position.y, location.z);
        }

        GameObject itemInstance =
            Instantiate(itemPrefab, spawnPosition, Quaternion.identity);
        
        int maxAttempts = 50;
        int attempts = 0;
        int r = -1;

        while ((r == -1 || !spawnableItems[r].CanUse) && attempts < maxAttempts)
        {
            r = Random.Range(0, spawnableItems.Length);
            attempts++;
        }

        if (attempts >= maxAttempts)
            r = 0;

        itemInstance.GetComponent<NetworkObject>().Spawn(true);
        itemInstance.GetComponent<Item>().SetupSpellClientRpc(r);

        spawnedItems.Add(itemInstance);
        currentAmount++;
    }


    public void ChangeMaxItemAmount(bool increase)
    {
        if (!NetworkManager.Singleton.IsServer) return;

        if (increase)
            maxAmount++;
        else
            maxAmount--;
    }

    public void ResetSpawner()
    {
        if (!NetworkManager.Singleton.IsServer) return;

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

    public SO_Spell GetSpellByIndex(int index)
    {
        if (index < 0 || index >= spawnableItems.Length) return null;
        return spawnableItems[index];
    }
    public int GetSpellCount()
    {
        return spawnableItems.Length;
    }

    private void OnApplicationQuit()
    {
        foreach (SO_Spell spell in spawnableItems)
        {
            spell.CanUse = true;
        }
    }
}