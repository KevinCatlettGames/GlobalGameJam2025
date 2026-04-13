using Unity.Netcode;
using UnityEngine;

public class SoapMapEvent : MapEvent
{
    [SerializeField] private GameObject soapDroplet;
    [SerializeField] private float spawnRadius = 21f;
    [SerializeField] private float startDelay = 10f;
    [SerializeField] private float startInvterval = 2f;
    [SerializeField] private float minInterval = .3f;
    [SerializeField] private float intervalAdjustment = -.1f;

    private float currentIntervall = 0;
    private bool isSpawning = false;
    protected override void StartEvent()
    {
        currentIntervall = startInvterval;
        isSpawning = true;
        Invoke(nameof(SpawnDroplet), currentIntervall + startDelay);
    }

    protected override void StopEvent()
    {
        isSpawning = false;
        CancelInvoke();
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
}
