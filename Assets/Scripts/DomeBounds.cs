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

    private void OnTriggerEnter(Collider other)
    {   

        // Check if the object is a player
        if (other.CompareTag("Player"))
        {   
            other.gameObject.GetComponent<PlayerController>().Die();
        }
    }
}
