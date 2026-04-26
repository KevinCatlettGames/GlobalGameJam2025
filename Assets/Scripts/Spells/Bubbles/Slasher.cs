using System;
using System.Collections.Generic;
using UnityEngine;

public class Slasher : MonoBehaviour
{
    [SerializeField] private SlashBubble slasherBubble;
    private bool inflated = false;
    private List<PlayerController> team;
    private List<Collider> ignoredColliders = new List<Collider>();

    public void SetInflated(Collider playerCollider, int ID)
    {
        SphereCollider sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider != null)
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
                Physics.IgnoreCollision(sphereCollider, col);
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
            gameObject.SetActive(false);
            return;
        }
        if(other.gameObject != null)
            slasherBubble.SlasherHit(transform.forward, other.gameObject);
    }
}
