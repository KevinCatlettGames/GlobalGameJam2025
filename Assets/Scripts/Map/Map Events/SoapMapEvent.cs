using System.Collections;
using System.Collections.Generic;
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
        maxWaveSize = droplets.Length;
        startWaveSize = waveSize;
    }
    protected override void StartEvent()
    {
        isSpawning = true;
        StartCoroutine(SpawnWaves());
    }

    protected override void StopEvent()
    {
        isSpawning = false;
        waveSize = startWaveSize;
        StopAllCoroutines();
    }
    private IEnumerator SpawnWaves()
    {
        yield return new WaitForSeconds(pauseDuration);
        while (isSpawning)
        {
            waringIndicator.SetActive(true);
            yield return new WaitForSeconds(1f);
            for (int i = 0; i < waveSize; i++)
            {
                Vector2 randomPos = Random.insideUnitCircle * spawnRadius;
                Vector3 spawnPosition = new Vector3(randomPos.x, 0, randomPos.y);
                droplets[i].ActivateDroplet(spawnPosition,waveDuration);
            }
            
            yield return new WaitForSeconds(waveDuration);
            waringIndicator.SetActive(false);
            
            if (waveSize < maxWaveSize)
            {
                waveSize += waveSizeIncrease;
                if (waveSize > maxWaveSize)
                    waveSize = maxWaveSize;
            }
            
            yield return new WaitForSeconds(pauseDuration);
        }
    }
}
