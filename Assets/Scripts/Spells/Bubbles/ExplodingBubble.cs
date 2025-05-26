using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode; 

public class ExplodingBubble : BasicBubble
{
    private BubbleExplosion bubbleExplosion;
    [SerializeField] private float explosionRadius = 5f;

    public override void InitialiseBubble(int ID, float dmg, float knb, float spd, float rng, float siz, float inf, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dmg, knb, spd, rng, siz, inf, dir, soundEvent, playerCollider);
        bubbleExplosion = GetComponentInChildren<BubbleExplosion>();
        bubbleExplosion.OwnerID = ID;

        if (GameManager.Instance.playingLocal)
            bubbleExplosion.SetExplosionSize(explosionRadius / size);
        else
            SetExplosionSizeServerRpc();
    }
    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped || !IsServer) return; 
        if (other.CompareTag("Player") && other.GetComponent<Collider>() != playerCollider)
        {
            PlayerController player = other.GetComponent<PlayerController>();
            
            if (GameManager.Instance.playingLocal)
                player.ApplyKnockbackLocal(OwnerID, direction, knockback, damage);
            else
                player.ApplyKnockbackServerRpc(OwnerID, direction, knockback, damage);
        }
        else if (other.CompareTag("Bubble"))
        {
            bubbleExplosion.OwnerID = other.GetComponent<BasicBubble>().OwnerID;
        }
        Pop();
    }
    protected override void Pop()
    {
        if (hasPopped) return;
        bubbleExplosion.Explode(knockback, damage);
        base.Pop();
    }
    
    
    [ServerRpc]
    void SetExplosionSizeServerRpc()
    {
        SetExplosionSizeClientRpc();
    }

    [ClientRpc]
    void SetExplosionSizeClientRpc()
    {
        Debug.Log("Set Explosion Client Rpc");
        bubbleExplosion.SetExplosionSize(explosionRadius / size);
    }

}
