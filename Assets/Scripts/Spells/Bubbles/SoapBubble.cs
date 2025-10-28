using System.Collections;
using UnityEngine;
using Unity.Netcode;
using Unity.VisualScripting;

public class SoapBubble : BasicBubble
{
    [SerializeField] private GameObject soapPuddleObject;
    [SerializeField] private GameObject soapSplatObject;
    [SerializeField] private float soapDropInterval = 0.2f;
    [SerializeField] private LayerMask groundedLayerMask;
    private const float raycastDistance = 5f;

    private float timer = 0;

    private void Update()
    {
        if (soapPuddleObject == null) return;
        timer += Time.deltaTime;
        if (timer >= soapDropInterval)
        {
            DropSoapPuddle(false);
            timer = 0f;
        }
    }

    private void DropSoapPuddle(bool hitPlayer)
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo, raycastDistance, groundedLayerMask))
        {
            if (IsServer)
            {
                GameObject puddle;
                if (hitPlayer) 
                    puddle = Instantiate(soapSplatObject, hitInfo.point, transform.rotation);
                else
                    puddle = Instantiate(soapPuddleObject, hitInfo.point, transform.rotation);
                puddle.GetComponent<NetworkObject>()?.Spawn();
            }
        }
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

            DropSoapPuddle(true);
        }
        Pop();
    }

    public override void SetSlippy()
    {
        return;
    }
}