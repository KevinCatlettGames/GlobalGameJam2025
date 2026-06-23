using System.Collections.Generic;
using UnityEngine;

public class Slasher : MonoBehaviour
{
    [SerializeField] private SlashBubble slasherBubble;
    private bool inflated = false;
    private List<Collider> ignoredColliders = new List<Collider>();
    [SerializeField] private Collider collider;
    [SerializeField] private GameObject slasherParent;
    [SerializeField] private GameObject popEffect;
    [SerializeField] private GameObject hitEffect;
    private bool hasPopped = false;

    public void SetInflated(Collider playerCollider, int ID)
    {
        if (collider != null)
        {
            List<PlayerController> team = GameManager.Instance.GetTeam(ID);
            if (team != null)
            {
                foreach (PlayerController player in team)
                {
                    if (player != null)
                        ignoredColliders.Add(player.Controller);
                }
            }
            else
            {
                if (playerCollider != null)
                    ignoredColliders.Add(playerCollider);
            }

            foreach (Collider col in ignoredColliders)
            {
                Physics.IgnoreCollision(collider, col);
            }
        } 
        inflated = true;
    }

    public void OnTriggerEnter(Collider other)
    {
        if(!inflated)
            return;
        if(other.CompareTag("Wall"))
        {
            SpawnPopEffect();
            slasherParent.SetActive(false);
            return;
        }
        if(other.gameObject != null && !other.isTrigger)
        {
            SpawnHitEffect();
            slasherBubble.SlasherHit(transform.forward, other.gameObject);
        }
    }

    public void SpawnPopEffect()
    {
        if (popEffect && !hasPopped)
        {
            hasPopped = true;
            Instantiate(popEffect, collider.transform.position, Quaternion.identity);
        }
    }

    public void SpawnHitEffect()
    {
        if (hitEffect)
            Instantiate(hitEffect, collider.transform.position, Quaternion.identity);
    }

    private void OnDestroy()
    {
        SpawnPopEffect();
    }
}
