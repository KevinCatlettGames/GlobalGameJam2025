using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlipTrigger : MonoBehaviour
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
