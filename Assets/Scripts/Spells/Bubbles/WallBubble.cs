using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallBubble : BasicBubble
{
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
