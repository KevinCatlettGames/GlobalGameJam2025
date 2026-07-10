using Unity.Netcode;
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
        if (!IsServer && !isLocalFake) return; // Allow processing for the server and local fakes

        // --- LOCAL FAKE SHORT CIRCUIT ---
        if (isLocalFake)
        {
            // Instantly transition to explosion visual effects locally on impact without processing teleportation or state logic
            if (other.CompareTag("Player") || other.CompareTag("Bubble") || other.CompareTag("Wall") || other.CompareTag("Environment"))
            {
                Pop();
            }
            return;
        }

        // --- AUTHORITATIVE SERVER COLLISION DETECTION ---
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<PlayerController>();
            GameManager gameManager = GameManager.Instance;

            if (gameManager.PlayingLocal)
                player.ApplyKnockbackLocal(OwnerID, direction, knockback, damage);
            else
                player.ApplyKnockbackServerRpc(OwnerID, direction, knockback, damage);

            gameManager.ChangeHitReference(OwnerID, spellType, player.PlayerID, isSoaped, isReflected);

            if (playerCollider != null)
            {
                PlayerController playerController = playerCollider.GetComponent<PlayerController>();
                if (playerController != null)
                {
                    if (!isUlt) playerController.GainUltCharge(damage, true);
                    fizzleEffect = hitEffect;
                    Explode();
                    playerController.Teleport(other.transform.position - teleportOffset * direction);
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
        if (!IsServer) return; // Explicit safety fallback to avoid local simulation execution
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
                        player.ApplyKnockbackLocal(OwnerID, direction, explosionKnockback, explosionDamage);
                    else
                        player.ApplyKnockbackServerRpc(OwnerID, direction, explosionKnockback, explosionDamage);

                    gameManager.ChangeHitReference(OwnerID, spellType, player.PlayerID, isSoaped, isReflected);

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