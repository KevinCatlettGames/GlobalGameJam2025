using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class SlipTrigger : NetworkBehaviour
{
    private List<PlayerController> sliperyPlayers = new List<PlayerController>();
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.gameObject.GetComponent<PlayerController>();
            player.SetSlippy(true);
            sliperyPlayers.Add(player);
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
            player.SetSlippy(false);
            sliperyPlayers.Remove(player);
        }
       
    }
    private void OnDestroy()
    {
        foreach (PlayerController player in sliperyPlayers)
        {
            player.SetSlippy(false);
        }
    }
}
