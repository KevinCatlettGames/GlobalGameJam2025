using Unity.Netcode;
using UnityEngine;

public class DestroyAfterDuration : NetworkBehaviour
{
    [SerializeField] private float waitDuration;

    private void Start()
    {
        if (IsServer)
        {
            Invoke(nameof(DespawnNetworkObject), waitDuration);
        }
    }

    private void DespawnNetworkObject()
    {
        if (NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true); // true = destroy object after despawn
        }
    }
}