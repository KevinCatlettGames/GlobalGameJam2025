using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnipeBubble : BasicBubble
{
    public override void BubbleCollision(GameObject other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            player.ApplyKnockback(direction, knockback, damage);
            Pop();
        }
    }
}
