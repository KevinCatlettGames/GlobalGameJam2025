using UnityEngine;

public class CrossBubble : BasicBubble
{
    public override void BubbleCollision(GameObject other)
    {
        if (other.CompareTag("Bubble"))
        {
            BasicBubble bubble = other.GetComponent<BasicBubble>();
            if (bubble != null && bubble.spellType == SpellType.Cross && bubble.OwnerID == OwnerID)
            {
                Physics.IgnoreCollision(other.GetComponent<SphereCollider>(), sphereCollider, true);
                return;
            }
        }
        base.BubbleCollision(other);
    }
}
