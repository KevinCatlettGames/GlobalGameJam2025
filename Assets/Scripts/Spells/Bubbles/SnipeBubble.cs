using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnipeBubble : BasicBubble
{
    [SerializeField] private float minDamage = 10f;
    [SerializeField] private float damageRampUpDistance = 25f;
    private float damageScaleing = 0f;
    private float maxDamage = 0f;

    public override void InitialiseBubble(int ID, float dmg, float knb, float spd, float rng, float siz, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dmg, knb, spd, rng, siz, dir, soundEvent, playerCollider);
        maxDamage = dmg;
        damage = minDamage;
        damageScaleing = (maxDamage - minDamage) / damageRampUpDistance;
    }
    protected override void BubbleMovement()
    {
        base.BubbleMovement();
        if (damage < maxDamage)
        {
            damage += speed * Time.fixedDeltaTime * damageScaleing;
            if (damage > maxDamage) damage = maxDamage; 
        }
    }
    public override void BubbleCollision(GameObject other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            player.ApplyKnockback(OwnerID, direction, knockback, damage);
            Pop();
        }
        else if (other.CompareTag("Bubble"))
        {
            if (other.TryGetComponent<SnipeBubble>(out SnipeBubble snipeComponent))
            {
                snipeComponent.Pop();
                Pop();
            }
        }
    }
}
