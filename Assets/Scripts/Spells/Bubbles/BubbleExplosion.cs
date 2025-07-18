using System.Collections;
using System.Collections.Generic;
using Unity.Netcode; 
using UnityEngine;

public class BubbleExplosion : NetworkBehaviour
{
    [SerializeField] private GameObject indicator;
    [HideInInspector] public int OwnerID;
    private float explosionRadius = 0;
    private bool hasExploded = false;

    public void Explode(float knockback, float damage)
    {
        if (hasExploded) return;
        hasExploded = true;
        Collider[] explosionOverlaps = Physics.OverlapSphere(transform.position, explosionRadius, LayerMask.GetMask("Bubble", "Player"));
        Vector3 origin;
        Vector3 direction;
        foreach (Collider col in explosionOverlaps)
        {
            if (col == null || col.gameObject == this.transform.parent.gameObject) continue;
            origin = transform.position;
            direction = col.transform.position - transform.position;
            Debug.Log(col.name);
            if (!Physics.Raycast(origin, direction, direction.magnitude, LayerMask.GetMask("Wall")))
            {
                if (col.CompareTag("Player"))
                {
                    PlayerController player = col.GetComponent<PlayerController>();
                    if (player != null)
                    {
                        if (GameManager.Instance.PlayingLocal)
                            player.ApplyKnockbackLocal(OwnerID, direction, knockback, damage);
                        else
                            player.ApplyKnockbackServerRpc(OwnerID, direction, knockback, damage);
                    }
                }
                else
                {
                    BasicBubble bubble = col.GetComponent<BasicBubble>();
                    if (bubble != null)
                    {
                        bubble.BubbleCollision(this.gameObject);
                    }
                }

            }
        }
    }
    public void SetExplosionSize(float radius, float size)
    {
        explosionRadius = radius;
        indicator.transform.localScale *= radius / size;
    }
}
