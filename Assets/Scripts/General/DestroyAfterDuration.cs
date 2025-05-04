using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class DestroyAfterDuration : NetworkBehaviour
{
    [SerializeField] private float waitDuration = 5f;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            StartCoroutine(DespawnAfterDelay(waitDuration));
        }
    }

    private IEnumerator DespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true); // true = destroy the GameObject after despawn
        }
    }
}