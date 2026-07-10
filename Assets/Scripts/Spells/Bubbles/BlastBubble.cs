using FMODUnity;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;

public class BlastBubble : BasicBubble
{
    [Header("Special Stats")]
    [SerializeField] private GameObject splat;
    [SerializeField] private LayerMask groundedLayerMask;
    [SerializeField] private float extraOffset = 4.5f;
    [SerializeField] private float shooterKnb = 8f;
    private const float raycastDistance = 5f;

    public override void InitialiseBubble(int ID, Vector3 dir, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dir, playerCollider);
        transform.position += direction * extraOffset;

        // --- PREDICTION IMPROVEMENT ---
        // Give the shooter instant recoil satisfaction! 
        // Run this if it's a local match OR if it's the prediction fake on the shooter's screen.
        if (GameManager.Instance.PlayingLocal || isLocalFake)
        {
            playerCollider.GetComponent<PlayerController>().ApplyImpulseLocal(direction * -1, shooterKnb);
        }
        else if (IsServer)
        {
            // The server still runs this to keep other players in sync if needed, 
            // but the NetworkHide handles separating the visual simulation layers.
            playerCollider.GetComponent<PlayerController>().ApplyImpulseServerRpc(direction * -1, shooterKnb);
        }
    }

    protected override void InflateOverlapChack()
    {
        Collider[] overlaps = Physics.OverlapSphere(transform.position, size, LayerMask.GetMask("Player", "Bubble"));

        foreach (Collider col in overlaps)
        {
            if (ignoredColliders.Contains(col)) continue;

            // --- LOCAL FAKE MODE GATE ---
            if (isLocalFake)
            {
                // Local fake just registers that it hit an active element and pops cleanly
                if (col.CompareTag("Player") || col.CompareTag("Bubble"))
                {
                    Pop();
                    return;
                }
                continue;
            }

            // --- AUTHORITATIVE SERVER LOGIC ---
            if (col.CompareTag("Player"))
            {
                var player = col.GetComponent<PlayerController>();
                GameManager gameManager = GameManager.Instance;

                if (gameManager.PlayingLocal)
                    player.ApplyKnockbackLocal(OwnerID, direction, knockback, damage);
                else
                    player.ApplyKnockbackServerRpc(OwnerID, direction, knockback, damage);

                gameManager.ChangeHitReference(OwnerID, spellType, player.PlayerID, isSoaped, isReflected);
                if (!isUlt && playerCollider != null) playerCollider.GetComponent<PlayerController>().GainUltCharge(damage, true);
                fizzleEffect = hitEffect;
            }
            else if (col.CompareTag("Bubble"))
            {
                col.GetComponent<BasicBubble>()?.BubbleCollision(gameObject);
            }
        }

        Pop();
    }

    public override void BubbleCollision(GameObject other)
    {
        // Maintains original structure
        return;
    }

    protected override void Pop()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo, raycastDistance, groundedLayerMask))
        {
            // --- SERVER GATE ---
            // Only spawn the replicated ink puddle if this is the actual authoritative server bubble!
            if (IsServer)
            {
                GameObject puddle = Instantiate(splat, hitInfo.point, transform.rotation);
                puddle.GetComponent<NetworkObject>()?.Spawn();
                puddle.GetComponent<InkTrigger>()?.SetOwner(OwnerID);
            }
        }

        // Pass to base.Pop() which now handles instant visual culling via HideVisualsAndDisablePhysics()
        base.Pop();
    }
}