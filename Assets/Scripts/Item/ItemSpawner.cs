using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class ItemSpawner : MonoBehaviour
{
    public static ItemSpawner Instance;

    [SerializeField] GameObject itemPrefab;
    [SerializeField] Transform[] startSpawnPoints;
    [SerializeField] float spawnInterval = 5;
    [SerializeField] float startDelay = 5;
    [SerializeField] float missingItemIncrease = .25f;
    [SerializeField] float spawnRadius = 15;
    [SerializeField] private float side = 0f;
    [SerializeField] SO_Spell[] spawnableItems;
    [SerializeField] bool spawningEnabled = true;
    public SO_Spell[]  SpawnableItems { get { return spawnableItems; } }
    private List<int> legalSpells;
    
    public int maxAmount = 6;
    public int currentAmount;
    private float spawnTimer = 0;
    private bool isSpawning = false;

    private List<GameObject> spawnedItems = new List<GameObject>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);     
    }

    public void InitialSpawn(int currentPlayers)
    {
        if (NetworkManager.Singleton.IsServer && spawningEnabled)
        {
            GameManager.Instance.OnGameStarted += ResetSpawner;
            GameManager.Instance.OnGameEnded += StopSpawning;

            legalSpells = new List<int>();
            for (int i = 0; i < GetSpellCount(); i++)
            {
                if (SteamIntegration.instance && SteamIntegration.instance.IsFullVersion || !SteamIntegration.instance)
                {
                    if (SpawnableItems[i].CanUse)
                    {
                        legalSpells.Add(i);
                    }
                }
                else if (SteamIntegration.instance && !SteamIntegration.instance.IsFullVersion)
                {
                    if (SpawnableItems[i].CanUse && SpawnableItems[i].AvailableInDemo)
                    {
                        legalSpells.Add(i);
                    }
                }
            }

            foreach (Transform t in startSpawnPoints)
                SpawnItem(t.position);

            Invoke(nameof(StartSpawning), startDelay);
        }
    }
    private void Update()
    {
        if (isSpawning && spawningEnabled && currentAmount < maxAmount)
        {
            if (spawnTimer <= 0)
            {
                SpawnItem(Vector3.zero);
                spawnTimer = spawnInterval;
            }
            else
            {
                float f = -(currentAmount - maxAmount);
                spawnTimer -= Time.deltaTime * (1 + missingItemIncrease * f);
            }
        }
    }
    private void StartSpawning()
    {
        if (NetworkManager.Singleton && !NetworkManager.Singleton.IsServer) return;
        isSpawning = true;
        spawnTimer = spawnInterval;
    }
    private void StopSpawning()
    {
        if (NetworkManager.Singleton && !NetworkManager.Singleton.IsServer) return;
        isSpawning = false;
        spawnTimer = spawnInterval;
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
        
        int spellID = GetRandomLegalSpellID();

        itemInstance.GetComponent<NetworkObject>().Spawn(true);
        itemInstance.GetComponent<Item>().SetupSpellClientRpc(spellID);

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

        Invoke(nameof(StartSpawning), startDelay);
    }

    public SO_Spell GetSpellByIndex(int index)
    {
        if (index < 0 || index >= spawnableItems.Length) return null;
        return spawnableItems[index];
    }

    public int GetRandomLegalSpellID()
    {
        int r = Random.Range(0, legalSpells.Count);
        return legalSpells[r];
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