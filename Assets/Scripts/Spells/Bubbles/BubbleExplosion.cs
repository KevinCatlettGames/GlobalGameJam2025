using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode; 

public class BubbleExplosion : NetworkBehaviour
{
    private List<PlayerController> effectedPlayers = new List<PlayerController>();
    private List<BasicBubble> effectedBubbles = new List<BasicBubble>();
    [SerializeField] private GameObject indicator;
    [HideInInspector] public int OwnerID;

    public void Explode(float knockback, float damage)
    {
        if (effectedPlayers.Count > 0)
        {
            foreach (PlayerController player in effectedPlayers)
            {
                if (player != null)
                {
                    Vector3 kockbackDirection = player.transform.position - transform.position;
                    if (GameManager.Instance.PlayingLocal)
                        player.ApplyKnockbackLocal(OwnerID, kockbackDirection, knockback, damage);
                    else
                        player.ApplyKnockbackServerRpc(OwnerID, kockbackDirection, knockback, damage);
                }
            }
        }
        if (effectedBubbles.Count > 0)
        {
            foreach (BasicBubble bubble in effectedBubbles)
            {
                if (bubble != null)
                {
                    bubble.BubbleCollision(this.gameObject);
                }
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if(!effectedPlayers.Contains(player)) effectedPlayers.Add(other.GetComponent<PlayerController>());
        }
        else if (other.CompareTag("Bubble"))
        {
            BasicBubble bubble = other.GetComponent<BasicBubble>();
            if(!effectedBubbles.Contains(bubble)) effectedBubbles.Add(bubble);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            effectedPlayers.Remove(other.GetComponent<PlayerController>());
        }
        else if (other.CompareTag("Bubble"))
        {
            effectedBubbles.Remove(other.GetComponent<BasicBubble>());
        }
    }

    public void SetExplosionSize(float radius)
    {
        SphereCollider explosionCollider = GetComponent<SphereCollider>();
        explosionCollider.radius = radius;
        indicator.transform.localScale *= radius;
    }
}
