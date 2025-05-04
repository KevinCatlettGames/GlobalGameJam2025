using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SlipTrigger : NetworkBehaviour
{
    private List<PlayerController> slipperyPlayers = new List<PlayerController>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.gameObject.GetComponent<PlayerController>();
            if (IsServer)
            {
                player.SetSlippy(true);
                slipperyPlayers.Add(player);
            }
        }
        else if (other.CompareTag("Bubble"))
        {
            BasicBubble bubble = other.GetComponent<BasicBubble>();
            bubble.SetSlippy();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.gameObject.GetComponent<PlayerController>();
            if (IsServer)
            {
                player.SetSlippy(false);
                slipperyPlayers.Remove(player);
            }
        }
    }

    private void OnDestroy()
    {
        if (IsServer)
        {
            foreach (PlayerController player in slipperyPlayers)
            {
                player.SetSlippy(false);
            }
        }
    }
}