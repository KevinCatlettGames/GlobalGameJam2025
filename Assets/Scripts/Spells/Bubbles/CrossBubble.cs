using UnityEngine;

public class CrossBubble : BasicBubble
{
    public override void BubbleCollision(GameObject other)
    {
        // Safety check if the other object is null
        if (other == null)
        {
            base.BubbleCollision(other);
            return;
        }

        // Check if we hit another bubble (handles standard tags or local visual fake tags)
        if (other.CompareTag("Bubble"))
        {
            BasicBubble bubble = other.GetComponent<BasicBubble>();
            if (bubble != null && bubble.spellType == SpellType.Cross && bubble.OwnerID == OwnerID)
            {
                // Safely grab both colliders to skip physics calculations
                SphereCollider otherCollider = other.GetComponent<SphereCollider>();
                if (otherCollider != null && sphereCollider != null)
                {
                    Physics.IgnoreCollision(otherCollider, sphereCollider, true);
                }
                return; // Exit out early so the cross bubbles pass cleanly through each other!
            }
        }

        // Pass any non-cross bubble collisions (walls, enemies, different spells) to the base architecture
        base.BubbleCollision(other);
    }
}