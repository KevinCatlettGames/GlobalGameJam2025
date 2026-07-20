using System.Collections.Generic;
using UnityEngine;

public class InkTrigger : MonoBehaviour 
{
    private List<PlayerController> inkedPlayers = new List<PlayerController>();
    [SerializeField] private float bubbleSlowFactor = .5f;
    private int ownerID = -1;

    public void SetOwner(int ID)
    {
        ownerID = ID;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.gameObject.GetComponent<PlayerController>();
            if (player.PlayerID != ownerID)
            {
                player.SetSlowed(true);
                inkedPlayers.Add(player);
            }
        }
        else if (other.CompareTag("Bubble"))
        {
            BasicBubble bubble = other.GetComponent<BasicBubble>();
            if (bubble && bubble.OwnerID.Value != ownerID)
            {
                bubble.ChangeSpeed(bubbleSlowFactor);
                Debug.Log("Slow bubble");
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.gameObject.GetComponent<PlayerController>();
            if (inkedPlayers.Contains(player))
                inkedPlayers.Remove(player);

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
