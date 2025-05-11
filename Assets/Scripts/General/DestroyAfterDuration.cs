using System;
using UnityEngine;
using System.Collections;

public class DestroyAfterDuration : MonoBehaviour
{
    [SerializeField] private float waitDuration = 5f;

    private void Awake()
    {
        StartCoroutine(DespawnAfterDelay(waitDuration));
    }

    private IEnumerator DespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        Destroy(gameObject);
    }
}