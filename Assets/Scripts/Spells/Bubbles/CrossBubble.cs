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
            if (bubble != null && bubble.spellType == SpellType.Cross && bubble.OwnerID.Value == OwnerID.Value)
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

    protected override bool DetectsImpact(out Vector3 impactPoint)
    {
        impactPoint = transform.position;

        float lookAheadDistance = (speed * Time.fixedDeltaTime) + 0.05f;
        float checkRadius = (sphereCollider != null) ? (sphereCollider.radius * transform.localScale.x * 0.5f) : (currentSize * 0.5f);

        if (Physics.SphereCast(transform.position, checkRadius, direction, out RaycastHit hit, lookAheadDistance))
        {
            // Ignore team/self colliders
            if (ignoredColliders.Contains(hit.collider) || hit.collider.transform.root == transform.root)
                return false;

            // Ignore hits against friendly/owned CrossBubbles during local prediction
            if (hit.collider.CompareTag("Bubble"))
            {
                BasicBubble otherBubble = hit.collider.GetComponent<BasicBubble>();
                if (otherBubble != null && otherBubble.spellType == SpellType.Cross && otherBubble.OwnerID.Value == OwnerID.Value)
                {
                    return false;
                }
            }

            impactPoint = hit.point;
            return true;
        }

        return false;
    }
}