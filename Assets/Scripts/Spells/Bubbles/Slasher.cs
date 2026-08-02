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
            // --- CLIENT/SERVER TEAM SAFE CHECK ---
            if (GameManager.Instance != null)
            {
                List<PlayerController> team = GameManager.Instance.GetTeam(ID);
                if (team != null)
                {
                    foreach (PlayerController player in team)
                    {
                        if (player != null && player.Controller != null)
                            ignoredColliders.Add(player.Controller);
                    }
                }
                else
                {
                    if (playerCollider != null)
                        ignoredColliders.Add(playerCollider);
                }
            }
            else if (playerCollider != null)
            {
                ignoredColliders.Add(playerCollider);
            }

            foreach (Collider col in ignoredColliders)
            {
                if (col != null)
                {
                    Physics.IgnoreCollision(collider, col);
                }
            }
        }
        inflated = true;
    }

    public void OnTriggerEnter(Collider other)
    {
        if (!inflated || other == null) return;

        // Block processing if the parent script is deactivated or missing
        if (slasherBubble == null) return;

        if (other.CompareTag("Wall"))
        {
            SpawnPopEffect();
            if (slasherParent != null) slasherParent.SetActive(false);
            return;
        }

        if (!other.isTrigger)
        {
            SpawnHitEffect();

            // Forwards direction and target to the parent SlashBubble.
            // Our prediction gates in SlashBubble.SlasherHit will intercept fake hits safely!
            slasherBubble.SlasherHit(transform.forward, other.gameObject);
        }
    }

    public void SpawnPopEffect()
    {
        if (popEffect && !hasPopped)
        {
            hasPopped = true;
            Vector3 spawnPos = collider != null ? collider.transform.position : transform.position;
            Instantiate(popEffect, spawnPos, Quaternion.identity);
        }
    }

    public void SpawnHitEffect()
    {
        if (hitEffect)
        {
            Vector3 spawnPos = collider != null ? collider.transform.position : transform.position;
            Instantiate(hitEffect, spawnPos, Quaternion.identity);
        }
    }

    private void OnDestroy()
    {
        SpawnPopEffect();
    }
}