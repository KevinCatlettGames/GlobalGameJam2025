using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class SoapMapEvent : MapEvent
{
    [SerializeField] private float spawnRadius = 21f;
    [SerializeField] private float waveDuration = 3f;
    [SerializeField] private float pauseDuration = 8f;
    [SerializeField] private int waveSize = 3;
    [SerializeField] private int waveSizeIncrease = 2;
    [SerializeField] private SoapDroplet[] droplets;
    [SerializeField] private GameObject waringIndicator;
    private bool isSpawning = false;
    private int maxWaveSize = 0;
    private int startWaveSize = 0;

    private void Start()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        maxWaveSize = droplets.Length;
        startWaveSize = waveSize;
    }
    protected override void StartEvent()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        isSpawning = true;
        StartCoroutine(SpawnWaves());
    }

    protected override void StopEvent()
    {
        if (!NetworkManager.Singleton.IsServer) return;
        isSpawning = false;
        waveSize = startWaveSize;
        StopAllCoroutines();
    }
    private IEnumerator SpawnWaves()
    {
        yield return new WaitForSeconds(pauseDuration);
        while (isSpawning)
        {
            ToggleWarningIndicatorServerRpc(true);
            yield return new WaitForSeconds(1f);
            for (int i = 0; i < waveSize; i++)
            {
                Vector2 randomPos = Random.insideUnitCircle * spawnRadius;
                Vector3 spawnPosition = new Vector3(randomPos.x, 0, randomPos.y);
                droplets[i].ActivateDroplet(spawnPosition,waveDuration);
            }
            
            yield return new WaitForSeconds(waveDuration);
            ToggleWarningIndicatorServerRpc(false);            
            if (waveSize < maxWaveSize)
            {
                waveSize += waveSizeIncrease;
                if (waveSize > maxWaveSize)
                    waveSize = maxWaveSize;
            }
            
            yield return new WaitForSeconds(pauseDuration);
        }
    }

    [ServerRpc]
    void ToggleWarningIndicatorServerRpc(bool value)
    {
        ToggleWarningIndicatorClientRpc(value);
    }

    [ClientRpc]
    void ToggleWarningIndicatorClientRpc(bool value)
    {
        waringIndicator.SetActive(value);
    }
}