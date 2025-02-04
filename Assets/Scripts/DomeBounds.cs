using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DomeBounds : MonoBehaviour
{
    private PlayerManager playerManager;

    private void Start()
    {
        // Get reference to the PlayerManager instance
        playerManager = PlayerManager.Instance;
    }

    private void OnTriggerExit(Collider other)
    {
        // Handle SlipBubble collisions
        if (other.TryGetComponent<SlipBubble>(out SlipBubble slipBubble))
        {
            slipBubble.BubbleCollision(this.gameObject);
            return;
        }

        // Check if the object is a player
        if (other.CompareTag("Player"))
        {   
            other.gameObject.GetComponent<PlayerController>().Die();
        }
    }
}
