using Unity.Netcode;
using UnityEngine;

public class SoapMapEvent : MonoBehaviour
{
    [SerializeField] private bool isMapEventEnabled = true;
    [SerializeField] private GameObject soapDroplet;
    [SerializeField] private float spawnRadius = 21f;
    [SerializeField] private float startInvterval = 2f;
    [SerializeField] private float minInterval = .3f;
    [SerializeField] private float intervalAdjustment = -.1f;

    private float currentIntervall = 0;
    private bool isSpawning = false;
    void Start()
    {
        if (!isMapEventEnabled) Destroy(this);
        if (NetworkManager.Singleton.IsServer)
        {
            GameManager.Instance.OnGameStarted += StartSpawning;
            GameManager.Instance.OnGameEnded += StopSpawning;
            StartSpawning();
        }
    }

   
    private void SpawnDroplet()
    {
        if (!isSpawning) return;
        Vector2 randomPos = Random.insideUnitCircle * spawnRadius;
        Vector3 spawnPosition = new Vector3(randomPos.x, 0, randomPos.y);
        GameObject droplet = Instantiate(soapDroplet, spawnPosition, Quaternion.identity);
        droplet.GetComponent<NetworkObject>()?.Spawn();
        if (currentIntervall > minInterval)
            currentIntervall += intervalAdjustment;
        Invoke(nameof(SpawnDroplet),currentIntervall);
    }

    private void StartSpawning()
    {
        currentIntervall = startInvterval;
        isSpawning = true;
        Invoke(nameof(SpawnDroplet), currentIntervall);
    }

    private void StopSpawning()
    {
        isSpawning = false;
        CancelInvoke();
    }
    private void OnDestroy()
    {
        if (NetworkManager.Singleton.IsServer)
        {
            GameManager.Instance.OnGameStarted -= StartSpawning;
            GameManager.Instance.OnGameEnded -= StopSpawning;
        }
    }
}
