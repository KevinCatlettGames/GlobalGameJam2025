using FMODUnity;
using System.Collections;
using UnityEngine;

public class GiantBubble : BasicBubble
{
    [SerializeField] private float extraOffset = 2f;

    private bool isGrowing = false;

    public override void InitialiseBubble(int ID, float dmg, float knb, float spd, float rng, float siz, float inf, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dmg, knb, spd, rng, siz, inf, dir, soundEvent, playerCollider);
        transform.position += direction * extraOffset;
    }
    protected override void BubbleMovement()
    {
        if (!sphereCollider.enabled) return;
        base.BubbleMovement();
    }
}