using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplodingBubble : BasicBubble
{
    private BubbleExplosion bubbleExplosion;
    [SerializeField] private bool indicator = true;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private GameObject earlyFizzleEffect;
    private bool isReadyToExpode = false;

    public override void InitialiseBubble(int ID, float dmg, float knb, float spd, float rng, float siz, float inf, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dmg, knb, spd, rng, siz, inf, dir, soundEvent, playerCollider);
        bubbleExplosion = GetComponentInChildren<BubbleExplosion>();
        bubbleExplosion.OwnerID = ID;
        if (indicator)
            bubbleExplosion.SetExplosionSize(explosionRadius, size);
    }
    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped || !IsServer) return; 
        if (other.CompareTag("Player") && other.GetComponent<Collider>() != playerCollider)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            
            if (GameManager.Instance.PlayingLocal)
                player.ApplyKnockbackLocal(OwnerID, direction, knockback, damage);
            else
                player.ApplyKnockbackServerRpc(OwnerID, direction, knockback, damage);
        }
        else if (other.CompareTag("Bubble"))
        {
            bubbleExplosion.OwnerID = other.GetComponent<BasicBubble>().OwnerID;
        }
        fizzleEffect = hitEffect;
        Pop();
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
        bubbleExplosion.ActivateIndicator(indicator);
        base.InflateOverlapChack();
    }
    protected override void Pop()
    {
        if (hasPopped) return;
        if(isReadyToExpode) bubbleExplosion.Explode(knockback, damage);
        else fizzleEffect = earlyFizzleEffect;
        base.Pop();
    }

}
