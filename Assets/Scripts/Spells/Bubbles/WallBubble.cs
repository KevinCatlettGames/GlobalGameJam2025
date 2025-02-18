using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallBubble : BasicBubble
{
    public override void InitialiseBubble(int ID, float dmg, float knb, float spd, float rng, float siz, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dmg, knb, spd, rng, siz, dir, soundEvent, playerCollider);
        GetComponent<Reflector>().OwnerID = ID;
    }
    protected override void BubbleMovement()
    {
        return;
    }

    public override void BubbleCollision(GameObject other)
    {
        if (other.CompareTag("Player"))
        {
            return;
        }
        Pop();
    }
}
