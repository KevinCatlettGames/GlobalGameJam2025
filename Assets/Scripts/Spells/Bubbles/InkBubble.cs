using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;

public class InkBubble : BasicBubble
{
    [SerializeField] private GameObject inkPuddle;
    [SerializeField] private LayerMask groundedLayerMask;
    private const float raycastDistance = 5f;
    private bool spawnInk = true;

    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped || other == null) return;
        if (!IsServer && !isLocalFake) return; // Allow both the authoritative server and local fakes to run collision math

        // Toggle state variable locally and on server if a projectile collision interrupts the ink dropping process
        if (other.CompareTag("Bubble") && popOnBubbleHit)
        {
            spawnInk = false;
        }

        // If it's a local fake, bypass server execution and pop visuals cleanly
        if (isLocalFake)
        {
            Pop();
            return;
        }

        base.BubbleCollision(other);
    }

    private void SpawnInk()
    {
        // --- SERVER ONLY GATE ---
        // Only the server has authority to instantiate networked objects into the global session
        if (!IsServer) return;

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo, raycastDistance, groundedLayerMask))
        {
            GameObject puddle = Instantiate(inkPuddle, hitInfo.point, transform.rotation);
            puddle.GetComponent<NetworkObject>()?.Spawn();
        }
    }

    protected override void Pop()
    {
        if (hasPopped) return;

        // Will only successfully generate on the server due to the IsServer check inside SpawnInk()
        if (spawnInk)
        {
            SpawnInk();
        }

        base.Pop();
    }
}