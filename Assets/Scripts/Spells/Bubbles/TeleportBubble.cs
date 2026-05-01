using Unity.Netcode;
using UnityEngine;

public class TeleportBubble : BasicBubble
{
    [Header("SpecialStats")]
    [SerializeField] private float explosionRadius = 2f;
    [SerializeField] private float explosionDamage = 6f;
    [SerializeField] private float explosionKnockback = 1f;
    private bool hasExploded = false;

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
            PlayerController playerController = playerCollider.GetComponent<PlayerController>();
            if (!isUlt) playerController.GainUltCharge(damage, true);
            fizzleEffect = hitEffect;
            Explode();
            playerController.Teleport(transform.position);

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
        if (hasExploded) return;
        hasExploded = true;
        fizzleEffect = hitEffect;
        Collider[] explosionOverlaps = Physics.OverlapSphere(transform.position, explosionRadius, LayerMask.GetMask("Bubble", "Player"));
        Vector3 origin;
        Vector3 direction;
        foreach (Collider col in explosionOverlaps)
        {
            if (!col || col.gameObject == gameObject) continue;
            origin = transform.position;
            direction = col.transform.position - transform.position;

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
                    playerCollider.GetComponent<PlayerController>().GainUltCharge(damage, true);
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
