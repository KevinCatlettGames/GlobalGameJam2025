using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplodingBubble : BasicBubble
{
    [Header("Special Stats")]
    [SerializeField] private bool indicator = true;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private GameObject earlyFizzleEffect;
    [SerializeField] private float primaryKnockbackIncrease = 1.2f;
    private bool isReadyToExpode = false;
    private bool hasExploded = false;
    private GameObject primaryTarget;

    public override void InitialiseBubble(int ID, Vector3 dir, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dir, playerCollider);
        canMiss = false;
    }

    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped) return;
        if (!IsServer && !isLocalFake) return; // Allow processing for the server and our local prediction fakes

        if (isLocalFake)
        {
            // Local fakes process the visual collision marker instantly and pop
            fizzleEffect = hitEffect;
            Pop();
            return;
        }

        // --- AUTHORITATIVE SERVER LOGIC ---
        if (other != null && other.CompareTag("Bubble") && popOnBubbleHit)
        {
            var otherBubble = other.GetComponent<BasicBubble>();
            if (otherBubble != null) OwnerID = otherBubble.OwnerID;
        }
        else if (other != null && other.CompareTag("Player"))
        {
            primaryTarget = other;
        }
        fizzleEffect = hitEffect;
        Pop();
    }

    protected override void InflateOverlapChack()
    {
        isReadyToExpode = true;
        base.InflateOverlapChack();
    }

    public void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        // If it's a local fake, we don't calculate or apply radial blast forces to players or other network objects
        if (isLocalFake) return;

        // --- AUTHORITATIVE SERVER EXPLOSION FORCE LOGIC ---
        Collider[] explosionOverlaps = Physics.OverlapSphere(transform.position, explosionRadius, LayerMask.GetMask("Bubble", "Player"));
        Vector3 origin;
        Vector3 direction;
        foreach (Collider col in explosionOverlaps)
        {
            if (col == null || col.gameObject == this.gameObject) continue;
            origin = transform.position;
            direction = col.transform.position - transform.position;
            if (!Physics.Raycast(origin, direction, direction.magnitude, LayerMask.GetMask("Wall")))
            {
                if (col.CompareTag("Player"))
                {
                    PlayerController player = col.GetComponent<PlayerController>();
                    if (player != null)
                    {
                        if (col.gameObject == primaryTarget)
                            knockback *= primaryKnockbackIncrease;

                        if (GameManager.Instance.PlayingLocal)
                            player.ApplyKnockbackLocal(OwnerID, direction, knockback, damage);
                        else
                            player.ApplyKnockbackServerRpc(OwnerID, direction, knockback, damage);

                        if (playerCollider != null)
                        {
                            var controller = playerCollider.GetComponent<PlayerController>();
                            if (controller != null) controller.GainUltCharge(damage, true);
                        }

                        if (col.gameObject == primaryTarget)
                            knockback /= primaryKnockbackIncrease;
                    }
                }
                else
                {
                    BasicBubble bubble = col.GetComponent<BasicBubble>();
                    if (bubble != null)
                    {
                        bubble.BubbleCollision(this.gameObject);
                    }
                }
            }
        }
    }

    protected override void Pop()
    {
        if (hasPopped) return;

        if (isReadyToExpode) Explode();
        else fizzleEffect = earlyFizzleEffect;

        // base.Pop() handles running the client RPC effects on the server, 
        // or instant visual culling if it's the client's local fake bubble.
        base.Pop();
    }
}