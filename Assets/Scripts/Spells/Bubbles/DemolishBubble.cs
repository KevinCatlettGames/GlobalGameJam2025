using UnityEngine;

public class DemolishBubble : BasicBubble
{
    [SerializeField] private float acceleration = .1f;

    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped || other == null) return;

        if (isLocalFake)
        {
            if (other.CompareTag("Wall"))
            {
                RisingWall risingWall = other.GetComponentInParent<RisingWall>();
                if (risingWall != null)
                {
                    risingWall.Sink(true);
                }
            }
            return;
        }

        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<PlayerController>();
            GameManager gameManager = GameManager.Instance;

            if (gameManager.PlayingLocal)
                player.ApplyKnockbackLocal(OwnerID.Value, direction, knockback, damage, isCrit);
            else
                player.ApplyKnockbackServerRpc(OwnerID.Value, direction, knockback, damage, isCrit);

            gameManager.ChangeHitReference(OwnerID.Value, spellType, player.PlayerID, isSoaped, isReflected, false);

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
    }

    protected override void BubbleMovement()
    {
        if (hasInflated)
        {
            speed *= 1 + (acceleration * Time.fixedDeltaTime);
        }
        base.BubbleMovement();
    }
}