using UnityEngine;

public class TeleportBubble : BasicBubble
{
    [Header("SpecialStats")]
    [SerializeField] private float teleportOffset = 3f;
    [SerializeField] private float explosionRadius = 2f;
    [SerializeField] private float explosionDamage = 6f;
    [SerializeField] private float explosionKnockback = 1f;
    private bool hasExploded = false;

    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped || other == null) return;
        if (!IsServer && !isLocalFake) return;

        if (isLocalFake)
        {
            if (other.CompareTag("Player") || other.CompareTag("Bubble") || other.CompareTag("Wall") || other.CompareTag("Environment"))
            {
                Pop();
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

            if (playerCollider != null)
            {
                PlayerController playerController = playerCollider.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    if (!isUlt) playerController.GainUltCharge(damage, true);
                    fizzleEffect = hitEffect;
                    Explode();
                    //playerController.Teleport(other.transform.position - teleportOffset * direction);
                }
            }

            if (popOnPlayerHit)
                Pop();
        }
        else if (other.CompareTag("Bubble"))
        {
            if (popOnBubbleHit)
                Pop();
        }
        else
        {
            Pop();
        }
    }

    private void Explode()
    {
        if (!IsServer) return;
        if (hasExploded) return;
        hasExploded = true;
        fizzleEffect = hitEffect;

        Collider[] explosionOverlaps = Physics.OverlapSphere(transform.position, explosionRadius, LayerMask.GetMask("Bubble", "Player"));
        foreach (Collider col in explosionOverlaps)
        {
            if (!col || col.gameObject == gameObject) continue;

            if (col.CompareTag("Player"))
            {
                GameManager gameManager = GameManager.Instance;
                PlayerController player = col.GetComponent<PlayerController>();
                if (player != null)
                {
                    if (gameManager.PlayingLocal)
                        player.ApplyKnockbackLocal(OwnerID.Value, direction, explosionKnockback, explosionDamage, isCrit);
                    else
                        player.ApplyKnockbackServerRpc(OwnerID.Value, direction, explosionKnockback, explosionDamage, isCrit);

                    gameManager.ChangeHitReference(OwnerID.Value, spellType, player.PlayerID, isSoaped, isReflected, false);

                    if (playerCollider != null)
                    {
                        playerCollider.GetComponent<PlayerController>()?.GainUltCharge(damage, true);
                    }
                }
            }
            else
            {
                BasicBubble bubble = col.GetComponent<BasicBubble>();
                if (bubble != null)
                {
                    bubble.BubbleCollision(gameObject);
                }
            }
        }
    }
}