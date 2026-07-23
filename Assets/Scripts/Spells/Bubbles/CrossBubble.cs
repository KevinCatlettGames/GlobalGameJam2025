using UnityEngine;

public class CrossBubble : BasicBubble
{
    public override void BubbleCollision(GameObject other)
    {
        if (other == null)
        {
            base.BubbleCollision(other);
            return;
        }

        if (other.CompareTag("Bubble"))
        {
            BasicBubble bubble = other.GetComponent<BasicBubble>();
            if (bubble != null && bubble.spellType == SpellType.Cross && bubble.OwnerID == OwnerID)
            {
                SphereCollider otherCollider = other.GetComponent<SphereCollider>();
                if (otherCollider != null && sphereCollider != null)
                {
                    Physics.IgnoreCollision(otherCollider, sphereCollider, true);
                }
                return;
            }
        }

        base.BubbleCollision(other);
    }
}