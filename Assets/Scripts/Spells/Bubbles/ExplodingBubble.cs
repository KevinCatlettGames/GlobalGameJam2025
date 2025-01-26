using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ExplodingBubble : BasicBubble
{
    [SerializeField] private BubbleExplosion bubbleExplosion;

    protected override void Pop()
    {
        if (hasPopped) return;
        bubbleExplosion.Explode(knockback, damage);
        base.Pop();
    }

}
