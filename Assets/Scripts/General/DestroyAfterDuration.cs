using Unity.Netcode;
using UnityEngine;

public class DestroyAfterDuration : NetworkBehaviour
{
    [SerializeField] private float waitDuration;
    
    // Start is called before the first frame update
    void Start()
    {
        if (IsServer)  // Check if this is running on the server
        {
            DestroyEffectAfterDurationServerRpc();
        }
    }

    // Function to destroy the object after waitDuration on the server
    [ServerRpc]
    private void DestroyEffectAfterDurationServerRpc()
    {
        Destroy(gameObject, waitDuration); // Destroy the object on the server
    }
}