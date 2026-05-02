using FMODUnity;
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

    public override void InitialiseBubble(int ID, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dir, soundEvent, playerCollider);
        canMiss = false;
    }
    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped || !IsServer) return; 
        if (other.CompareTag("Bubble") && popOnBubbleHit)
        {
            OwnerID = other.GetComponent<BasicBubble>().OwnerID;
        }
        else if (other.CompareTag("Player"))
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
                        playerCollider.GetComponent<PlayerController>().GainUltCharge(damage, true);
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
        if(isReadyToExpode) Explode();
        else fizzleEffect = earlyFizzleEffect;
        base.Pop();
    }

}
