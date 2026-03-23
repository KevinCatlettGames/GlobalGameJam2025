using UnityEngine;

public class RevolverBulletBubble : BasicBubble
{
    private RevolverBubble revolverBubble;
    public RevolverBubble RevolverBubble
    {
        get => revolverBubble;
        set => revolverBubble = value;
    }

    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped || !IsServer) return;

        if (other.CompareTag("Player"))
        {
            if (revolverBubble)
                revolverBubble.AddToHitCount();
        }
        else if (other.CompareTag("Bubble"))
        {
            if (other.TryGetComponent<RevolverBulletBubble>(out RevolverBulletBubble revolverComp))
            {
                if (revolverComp.OwnerID == OwnerID)
                    return;
            }
        }
        base.BubbleCollision(other);
    }
}
