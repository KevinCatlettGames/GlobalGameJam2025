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
        if (hasPopped || !IsServer) return;

        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            GameManager gameManager = GameManager.Instance;

            if (gameManager.PlayingLocal)
                player.ApplyKnockbackLocal(OwnerID, direction, knockback, damage);
            else
                player.ApplyKnockbackServerRpc(OwnerID, direction, knockback, damage);

            gameManager.hitReferences[OwnerID].spellType = spellType;
            gameManager.hitReferences[OwnerID].playerHitID = player.PlayerID;
            gameManager.hitReferences[OwnerID].wasSlippery = player.IsSlippery;

            if (revolverBubble)
                revolverBubble.AddToHitCount();
            
            Pop();
        }
        else if (other.CompareTag("Bubble"))
        {
            if (other.TryGetComponent<RevolverBulletBubble>(out RevolverBulletBubble revolverComp))
            {
                if (revolverComp.OwnerID == OwnerID)
                    return;
            }
            Pop();
        }
        else
        {
            Pop();
        }
    }
}
