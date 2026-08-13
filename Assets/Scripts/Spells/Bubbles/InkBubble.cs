using Unity.Netcode;
using UnityEngine;

public class InkBubble : BasicBubble
{
    [SerializeField] private GameObject inkPuddle;
    [SerializeField] private GameObject fakeInkPuddle;
    [SerializeField] private LayerMask groundedLayerMask;

    private const float raycastDistance = 5f;
    private bool spawnInk = true;

    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped || other == null) return;
        if (!IsServer && !isLocalFake) return; // Authoritative server and local predicted fakes run collision checks

        // Toggle ink spawning if an opposing projectile collision interrupts the ink process
        if (other.CompareTag("Bubble") && popOnBubbleHit)
        {
            spawnInk = false;
        }

        // Local fakes handle visual popping directly without invoking server RPCs
        if (isLocalFake)
        {
            Pop();
            return;
        }

        base.BubbleCollision(other);
    }

    private void SpawnInk()
    {
        if (!spawnInk) return;

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo, raycastDistance, groundedLayerMask))
        {
            // Server instantiates and spawns the networked authoritative puddle
            if (IsServer)
            {
                GameObject puddle = Instantiate(inkPuddle, hitInfo.point, transform.rotation);
                puddle.GetComponent<NetworkObject>()?.Spawn();
                puddle.GetComponent<DamageField>()?.SetID(OwnerID.Value);
                puddle.GetComponent<Puddle>()?.InitialisePuddle(playerCollider);
            }
            // Local predicted fake spawns a non-networked client visual puddle
            else if (isLocalFake)
            {
                GameObject targetPrefab = fakeInkPuddle != null ? fakeInkPuddle : inkPuddle;
                GameObject puddle = Instantiate(targetPrefab, hitInfo.point, transform.rotation);

                Puddle puddleScript = puddle.GetComponent<Puddle>();
                if (puddleScript != null)
                {
                    puddleScript.isLocalFake = true;
                }
            }
        }
    }

    protected override void Pop()
    {
        if (hasPopped) return;

        SpawnInk();

        base.Pop();
    }
}