using UnityEngine;

public class HarpoonBubble : BasicBubble
{
    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped) return;
        direction *= -1;
        base.BubbleCollision(other);
    }
}
