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
        if (hasPopped || other == null) return;
        if (!IsServer && !isLocalFake) return;

        if (other.CompareTag("Bubble"))
        {
            if (other.TryGetComponent<RevolverBulletBubble>(out RevolverBulletBubble revolverComp))
            {
                if (revolverComp.OwnerID == OwnerID)
                    return;
            }
        }

        if (isLocalFake)
        {
            if (other.CompareTag("Player") || other.CompareTag("Wall"))
                Pop();
            return;
        }

        if (other.CompareTag("Player"))
        {
            if (revolverBubble != null)
                revolverBubble.AddToHitCount();
        }

        base.BubbleCollision(other);
    }
}