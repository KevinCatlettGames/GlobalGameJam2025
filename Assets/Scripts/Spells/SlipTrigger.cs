using System.Collections.Generic;
using Unity.Netcode; 
using UnityEngine;
using UnityEngine.SceneManagement; 

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
            
            if (!bubble.IsSoaped && !bubble.GetComponent<SoapBubble>())
                CheckMakeBubbleSlipperyAchievement(bubble);

            bubble.SetSlippy();
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
            || !AchievementSaveSystem.instance || SceneManager.GetActiveScene().buildIndex == 5 || SceneManager.GetActiveScene().buildIndex == 6) return;

        AchievementSaveSystem achSaveSystem = AchievementSaveSystem.instance;
        achSaveSystem.IncrementStat(4,1);
    }
}