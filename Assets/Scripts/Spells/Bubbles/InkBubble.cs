using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;

public class InkBubble : BasicBubble
{
    [SerializeField] private GameObject inkPuddle;
    [SerializeField] private LayerMask groundedLayerMask;
    private const float raycastDistance = 5f;
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

            gameManager.ChangeHitReference(OwnerID, spellType, player.PlayerID, isSoaped, isReflected);

            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo, raycastDistance, groundedLayerMask))
            {
                if (IsServer)
                {
                    GameObject puddle;
                    puddle = Instantiate(inkPuddle, hitInfo.point, transform.rotation);
                    puddle.GetComponent<NetworkObject>()?.Spawn();
                }
            }
        }
        Pop();
    }
}
