using FMODUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GiantBubble : BasicBubble
{
    [SerializeField] private int healthPoints = 3;
    private float healthIncrement = 0f;
    private float currentHealth = 1f;

    public override void InitialiseBubble(int ID, float dmg, float knb, float spd, float rng, float siz, float inf, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dmg, knb, spd, rng, siz, inf, dir, soundEvent, playerCollider);
        healthIncrement = 1f / (float)healthPoints;
    }

    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped || !IsServer) return; 
        
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            
            if (GameManager.Instance.playingLocal)
                player.ApplyKnockbackLocal(OwnerID ,direction, knockback * currentHealth, damage);
            else
                player.ApplyKnockbackServerRpc(OwnerID ,direction, knockback * currentHealth, damage);
         
            Pop();
        }
        else if (other.CompareTag("Bubble"))
        {
            DamageBubble();
        }
        else
        {
            Pop();
        }
    }
    private void DamageBubble()
    {
        currentHealth -= healthIncrement;
        if (currentHealth <= 0f)
        {
            Pop();
            return;
        }
        transform.localScale = size * currentHealth * Vector3.one;
        damage *= 2f;
        speed *= 2f;
    }
}
