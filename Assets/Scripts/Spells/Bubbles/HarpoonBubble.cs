using UnityEngine;

public class HarpoonBubble : BasicBubble
{
    [SerializeField] private float pullForce = 45f;

    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped || other == null) return;
        if (!IsServer && !isLocalFake) return; // Allow our server bubble and local prediction fakes to process

        // --- 1. SHARED PHYSICS MODIFICATION (Bypasses regular projectile collision) ---
        if (other.CompareTag("Bubble"))
        {
            BasicBubble bubble = other.GetComponent<BasicBubble>();
            if (bubble != null && bubble.spellType != BasicBubble.SpellType.Wall)
            {
                Collider otherCollider = other.GetComponent<Collider>();
                if (sphereCollider != null && otherCollider != null)
                {
                    Physics.IgnoreCollision(sphereCollider, otherCollider);
                }
                return; // Let the harpoon pass cleanly through the projectile!
            }
        }

        // --- 2. LOCAL FAKE SHORT CIRCUIT ---
        if (isLocalFake)
        {
            // If the predictive fake bubble hits a player or a wall, visually pop it instantly
            if (other.CompareTag("Player") || other.CompareTag("Wall") || other.CompareTag("Environment"))
            {
                Pop();
            }
            return;
        }

        // --- 3. AUTHORITATIVE SERVER LOGIC ---
        if (other.CompareTag("Player"))
        {
            PlayerController targetPlayer = other.GetComponent<PlayerController>();
            if (targetPlayer != null)
            {
                // Pulls the hit player backward relative to the bubble's flight path (pulling them toward the shooter)
                if (GameManager.Instance.PlayingLocal)
                    targetPlayer.ApplyImpulseLocal(direction * -1, pullForce);
                else
                    targetPlayer.ApplyImpulseServerRpc(direction * -1, pullForce);
            }
        }

        // Pass the remaining collision events (like wall impacts) down to the base architecture
        base.BubbleCollision(other);
    }
}