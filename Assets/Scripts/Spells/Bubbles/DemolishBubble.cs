using UnityEngine;
using static UnityEditor.PlayerSettings;

public class DemolishBubble : BasicBubble
{
    [SerializeField] private float acceleration = .1f;

    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped) return;

        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<PlayerController>();
            GameManager gameManager = GameManager.Instance;

            if (gameManager.PlayingLocal)
                player.ApplyKnockbackLocal(OwnerID, direction, knockback, damage);
            else
                player.ApplyKnockbackServerRpc(OwnerID, direction, knockback, damage);

            gameManager.ChangeHitReference(OwnerID, spellType, player.PlayerID, isSoaped, isReflected);

            var effect = Instantiate(hitEffect, transform.position, Quaternion.identity);
        }
        else if (other.CompareTag("Wall"))
        {
            if (other.TryGetComponent<RisingWall>(out RisingWall risingWall))
            {
                risingWall.Sink(true);
                //Effect/Archievenemt can go here
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
