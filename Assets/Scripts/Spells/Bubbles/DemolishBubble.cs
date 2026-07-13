using UnityEngine;

public class DemolishBubble : BasicBubble
{
    [SerializeField] private float acceleration = .1f;

    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped || other == null) return;

        // --- LOCAL FAKE SEPARATION ---
        if (isLocalFake)
        {
            if (other.CompareTag("Wall"))
            {
                RisingWall risingWall = other.GetComponentInParent<RisingWall>();
                if (risingWall != null)
                {
                    risingWall.Sink(true); // Let the wall sink instantly on the client's screen
                }
            }
            // Ignore player hit mechanics here; let base piercing logic continue
            return;
        }

        // --- AUTHORITATIVE SERVER LOGIC ---
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<PlayerController>();
            GameManager gameManager = GameManager.Instance;

            if (gameManager.PlayingLocal)
                player.ApplyKnockbackLocal(OwnerID, direction, knockback, damage);
            else
                player.ApplyKnockbackServerRpc(OwnerID, direction, knockback, damage);

            gameManager.ChangeHitReference(OwnerID, spellType, player.PlayerID, isSoaped, isReflected);

            if (hitEffect != null)
            {
                Instantiate(hitEffect, transform.position, Quaternion.identity);
            }
        }
        else if (other.CompareTag("Wall"))
        {
            RisingWall risingWall = other.GetComponentInParent<RisingWall>();
            if (risingWall != null)
            {
                risingWall.Sink(true);
            }
        }

        // Notice: No Pop() call here, which preserves its awesome piercing capability!
    }

    protected override void BubbleMovement()
    {
        // Allow the local visual fake bubble to accelerate identically to the server's version
        if (hasInflated)
        {
            speed *= 1 + (acceleration * Time.fixedDeltaTime);
        }
        base.BubbleMovement();
    }
}