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
        if (!IsServer && !isLocalFake) return; // Authoritative server and local predicted fakes handle collisions

        // Ignore collisions with sister bullet bubbles spawned by the same owner
        if (other.CompareTag("Bubble"))
        {
            if (other.TryGetComponent<RevolverBulletBubble>(out RevolverBulletBubble revolverComp))
            {
                if (revolverComp.OwnerID.Value == OwnerID.Value)
                    return;
            }
        }

        // Local fake handling (runs locally on predicted client without server hit counting)
        if (isLocalFake)
        {
            Pop();
            return;
        }

        // Server authority hit logic
        if (IsServer && other.CompareTag("Player"))
        {
            if (revolverBubble != null)
            {
                revolverBubble.AddToHitCount();
            }
        }

        base.BubbleCollision(other);
    }
}