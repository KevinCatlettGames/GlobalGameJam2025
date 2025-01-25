using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GiantBubble : BasicBubble
{
    [SerializeField] private int healthPoints = 3;
    private float healthIncrement = 0f;
    private float currentHealth = 1f;

    public override void InitialiseBubble(float dmg, float knb, float spd, float rng, float siz, Vector3 dir)
    {
        base.InitialiseBubble(dmg, knb, spd, rng, siz, dir);
        healthIncrement = 1f / (float)healthPoints;
    }

    public override void BubbleCollision(GameObject other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            player.ApplyKnockback(direction, knockback * currentHealth, damage * currentHealth);
            Pop();
        }
        else if (other.CompareTag("Bubble"))
        {
            DamageBubble();
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
    }
}
