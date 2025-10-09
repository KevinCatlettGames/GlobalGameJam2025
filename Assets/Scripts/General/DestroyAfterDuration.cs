using System;
using UnityEngine;
using System.Collections;

public class DestroyAfterDuration : MonoBehaviour
{
    [SerializeField] private float waitDuration;
    [SerializeField] private bool destroyOnRestart = true;

    private void Start()
    {
        if(destroyOnRestart) GameManager.Instance.OnGameStarted += DestroyOnRestart;
        
        StartCoroutine(DespawnAfterDelay(waitDuration));
    }

    private void DestroyOnRestart()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if(destroyOnRestart) GameManager.Instance.OnGameStarted -= DestroyOnRestart;
    }

    private IEnumerator DespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}