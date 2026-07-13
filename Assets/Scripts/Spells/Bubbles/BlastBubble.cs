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

        if (GameManager.Instance.PlayingLocal || isLocalFake)
            playerCollider.GetComponent<PlayerController>().ApplyImpulseLocal(direction * -1, shooterKnb);
        else if (IsServer)
            playerCollider.GetComponent<PlayerController>().ApplyImpulseServerRpc(direction * -1, shooterKnb);
    }

    protected override void InflateOverlapChack()
    {
        Collider[] overlaps = Physics.OverlapSphere(transform.position, size, LayerMask.GetMask("Player", "Bubble"));

        foreach (Collider col in overlaps)
        {
            if (ignoredColliders.Contains(col)) continue;

            if (isLocalFake)
            {
                if (col.CompareTag("Player") || col.CompareTag("Bubble"))
                {
                    Pop();
                    return;
                }
                continue;
            }

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
        return;
    }

    protected override void Pop()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo, raycastDistance, groundedLayerMask))
        {
            if (IsServer)
            {
                GameObject puddle = Instantiate(splat, hitInfo.point, transform.rotation);
                puddle.GetComponent<NetworkObject>()?.Spawn();
                puddle.GetComponent<InkTrigger>()?.SetOwner(OwnerID);
            }
        }
        base.Pop();
    }
}