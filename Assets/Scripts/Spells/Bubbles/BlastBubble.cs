using Unity.Netcode;
using UnityEngine;

public class BlastBubble : BasicBubble
{
    [Header("Special Stats")]
    [SerializeField] private GameObject splat;
    [SerializeField] private GameObject fakeSplat;
    [SerializeField] private LayerMask groundedLayerMask;
    [SerializeField] private float extraOffset = 4.5f;
    [SerializeField] private float shooterKnb = 8f;
    private const float raycastDistance = 5f;

    public override void InitialiseBubble(int ID, Vector3 dir, Collider playerCollider, int assignedSpellID, bool fakeWithServerCaster)
    {
        base.InitialiseBubble(ID, dir, playerCollider, assignedSpellID, fakeWithServerCaster);
        transform.position += direction * extraOffset;

        if (GameManager.Instance.PlayingLocal)
            playerCollider.GetComponent<PlayerController>().ApplyImpulseLocal(direction * -1, shooterKnb);
        else if(IsServer)
            playerCollider.GetComponent<PlayerController>().ApplyImpulseServerRpc(direction * -1, shooterKnb);
    }

    protected override void InflateOverlapChack()
    {
        Collider[] overlaps = Physics.OverlapSphere(transform.position, size, LayerMask.GetMask("Player", "Bubble"));

        foreach (Collider col in overlaps)
        {
            if (ignoredColliders.Contains(col)) continue;

            if (col.CompareTag("Player"))
            {
                var player = col.GetComponent<PlayerController>();
                GameManager gameManager = GameManager.Instance;

                if (gameManager.PlayingLocal)
                    player.ApplyKnockbackLocal(OwnerID.Value, direction, knockback, damage);
                else
                {
                    if(IsServer)
                        player.ApplyKnockbackServerRpc(OwnerID.Value, direction, knockback, damage);
                }

                if(IsServer)
                    gameManager.ChangeHitReference(OwnerID.Value, spellType, player.PlayerID, isSoaped, isReflected);

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
                puddle.GetComponent<Puddle>().InitialisePuddle(playerCollider);
                puddle.GetComponent<InkTrigger>()?.SetOwner(OwnerID.Value);
            }
            else if(isLocalFake)
            {
                GameObject puddle = Instantiate(fakeSplat, hitInfo.point, transform.rotation);
                puddle.GetComponent<Puddle>().isLocalFake = true;
            }
        }

        base.Pop();
    }
}