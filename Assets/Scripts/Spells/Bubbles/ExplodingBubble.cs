using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplodingBubble : BasicBubble
{
    [SerializeField] private BubbleExplosion bubbleExplosion;

    public override void InitialiseBubble(int ID, float dmg, float knb, float spd, float rng, float siz, float inf, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dmg, knb, spd, rng, siz, inf, dir, soundEvent, playerCollider);
        bubbleExplosion.OwnerID = ID;
    }
    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped) return;
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            player.ApplyKnockback(OwnerID, direction, knockback, damage);
        }
        else if (other.CompareTag("Bubble"))
        {
            bubbleExplosion.OwnerID = other.GetComponent<BasicBubble>().OwnerID;
        }
        Pop();
    }
    protected override void Pop()
    {
        if (hasPopped) return;
        bubbleExplosion.Explode(knockback, damage);
        base.Pop();
    }

}
