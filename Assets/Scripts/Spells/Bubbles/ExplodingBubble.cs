using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplodingBubble : BasicBubble
{
    private BubbleExplosion bubbleExplosion;
    [SerializeField] private float explosionRadius = 5f;

    public override void InitialiseBubble(int ID, float dmg, float knb, float spd, float rng, float siz, float inf, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dmg, knb, spd, rng, siz, inf, dir, soundEvent, playerCollider);
        bubbleExplosion = GetComponentInChildren<BubbleExplosion>();
        bubbleExplosion.OwnerID = ID;
        bubbleExplosion.SetExplosionSize(explosionRadius / size.Value);
    }
    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped.Value) return;
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            player.ApplyKnockbackServerRpc(OwnerID.Value, direction.Value, knockback, damage);
        }
        else if (other.CompareTag("Bubble"))
        {
            bubbleExplosion.OwnerID = other.GetComponent<BasicBubble>().OwnerID.Value;
        }
        Pop();
    }
    protected override void Pop()
    {
        if (hasPopped.Value) return;
        bubbleExplosion.Explode(knockback, damage);
        base.Pop();
    }

}
