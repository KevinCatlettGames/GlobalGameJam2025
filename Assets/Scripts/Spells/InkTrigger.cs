using System.Collections.Generic;
using UnityEngine;

public class InkTrigger : MonoBehaviour 
{
    private List<PlayerController> inkedPlayers = new List<PlayerController>();

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.gameObject.GetComponent<PlayerController>();

            player.SetSlowed(true);
            inkedPlayers.Add(player);

        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.gameObject.GetComponent<PlayerController>();

            player.SetSlowed(false);
            inkedPlayers.Remove(player);

        }
    }

    private void OnDestroy()
    {
        foreach (PlayerController player in inkedPlayers)
        {
            player.SetSlowed(false);
        }
    }
}
