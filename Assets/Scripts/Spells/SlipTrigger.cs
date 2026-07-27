using System.Collections.Generic;
using UnityEngine;
using Unity.Netcode; 

public class SlipTrigger : MonoBehaviour
{
    private List<PlayerController> slipperyPlayers = new List<PlayerController>();
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.gameObject.GetComponent<PlayerController>();
         
                player.SetSlippy(true);
                slipperyPlayers.Add(player);
            
        }
        else if (other.CompareTag("Bubble"))
        {
            BasicBubble bubble = other.GetComponent<BasicBubble>();
            bubble.SetSlippy();
            CheckMakeBubbleSlipperyAchievement(bubble);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.gameObject.GetComponent<PlayerController>();
          
                player.SetSlippy(false);
                slipperyPlayers.Remove(player);
            
        }
    }

    private void OnDestroy()
    {
            foreach (PlayerController player in slipperyPlayers)
            {
                player.SetSlippy(false);
            }
    }

    private void CheckMakeBubbleSlipperyAchievement(BasicBubble spedUpBubble)
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)spedUpBubble.OwnerID.Value
            || !SteamIntegration.instance) return;
        
        SteamIntegration steamIntegration = SteamIntegration.instance;
        SteamIntegration.instance.IncrementIntSteamStat(4,1);
    }
}