using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplodingBubble : BasicBubble
{
    [SerializeField] private bool indicator = true;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private GameObject earlyFizzleEffect;
    private bool isReadyToExpode = false;
    private bool hasExploded = false;

    public override void InitialiseBubble(int ID, float dmg, float knb, float spd, float rng, float siz, float inf, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dmg, knb, spd, rng, siz, inf, dir, soundEvent, playerCollider);
        canMiss = false;
    }
    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped || !IsServer) return; 
        if (other.CompareTag("Bubble") && popOnBubbleHit)
        {
            OwnerID = other.GetComponent<BasicBubble>().OwnerID;
        }
        fizzleEffect = hitEffect;

        base.BubbleCollision(other);
    }
    protected override void InflateOverlapChack()
    {
        Collider[] bubbleOverlaps = Physics.OverlapSphere(transform.position, size, LayerMask.GetMask("Bubble"));
        foreach (var col in bubbleOverlaps)
        {
            if  (col.gameObject.TryGetComponent<ExplodingBubble>(out ExplodingBubble ex))
            {
                if(ex == this) continue;
                Pop();
                return;
            }
        }
        isReadyToExpode = true;
        base.InflateOverlapChack();
    }
    public void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        Collider[] explosionOverlaps = Physics.OverlapSphere(transform.position, explosionRadius, LayerMask.GetMask("Bubble", "Player"));
        Vector3 origin;
        Vector3 direction;
        foreach (Collider col in explosionOverlaps)
        {
            if (col == null || col.gameObject == this) continue;
            origin = transform.position;
            direction = col.transform.position - transform.position;
            if (!Physics.Raycast(origin, direction, direction.magnitude, LayerMask.GetMask("Wall")))
            {
                if (col.CompareTag("Player"))
                {
                    PlayerController player = col.GetComponent<PlayerController>();
                    if (player != null)
                    {
                        if (GameManager.Instance.PlayingLocal)
                            player.ApplyKnockbackLocal(OwnerID, direction, knockback, damage);
                        else
                            player.ApplyKnockbackServerRpc(OwnerID, direction, knockback, damage);
                        playerCollider.GetComponent<PlayerController>().GainUltCharge(damage, true);
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
        if(isReadyToExpode) Explode();
        else fizzleEffect = earlyFizzleEffect;
        base.Pop();
    }

}
