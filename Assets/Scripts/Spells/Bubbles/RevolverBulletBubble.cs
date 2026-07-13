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
        if (!IsServer && !isLocalFake) return; // Allow both server instances and local visual fakes

        // --- 1. SHARED INDEPENDENT COLLISION MODIFIER ---
        // Friendly revolver bullets pass clean through each other without causing a pop
        if (other.CompareTag("Bubble"))
        {
            if (other.TryGetComponent<RevolverBulletBubble>(out RevolverBulletBubble revolverComp))
            {
                if (revolverComp.OwnerID == OwnerID)
                    return; // Early exit, do absolutely nothing!
            }
        }

        // --- 2. LOCAL FAKE SHORT CIRCUIT ---
        if (isLocalFake)
        {
            // If the local visual fake hits a player or environment, pop it instantly for game-feel
            if (other.CompareTag("Player") || other.CompareTag("Wall"))
            {
                Pop();
            }
            return;
        }

        // --- 3. AUTHORITATIVE SERVER LOGIC ---
        if (other.CompareTag("Player"))
        {
            if (revolverBubble != null)
            {
                revolverBubble.AddToHitCount();
            }
        }

        // Pass the remaining collision logic down to the base script setup
        base.BubbleCollision(other);
    }
}