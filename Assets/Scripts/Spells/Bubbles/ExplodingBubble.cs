using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplodingBubble : BasicBubble
{
    [SerializeField] private BubbleExplosion bubbleExplosion;

    public override void InitialiseBubble(int ID, float dmg, float knb, float spd, float rng, float siz, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dmg, knb, spd, rng, siz, dir, soundEvent, playerCollider);
        bubbleExplosion.OwnerID = ID;
    }
    protected override void Pop()
    {
        if (hasPopped) return;
        bubbleExplosion.Explode(knockback, damage);
        base.Pop();
    }

}
