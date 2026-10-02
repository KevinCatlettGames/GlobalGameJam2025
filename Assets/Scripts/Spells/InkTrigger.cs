using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class InkTrigger : NetworkBehaviour 
{
    private List<PlayerController> inkedPlayers = new List<PlayerController>();
    [SerializeField] private float bubbleSlowFactor = .5f;
    public NetworkVariable<int> ownerID = new NetworkVariable<int>();

    public void SetOwner(int ID)
    {
        ownerID.Value = ID;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.gameObject.GetComponent<PlayerController>();
            if (player.PlayerID != ownerID.Value)
            {
                player.SetSlowed(true);
                inkedPlayers.Add(player);
            }
        }
        else if (other.CompareTag("Bubble"))
        {
            BasicBubble bubble = other.GetComponent<BasicBubble>();
            if (bubble && bubble.OwnerID.Value != ownerID.Value)
            {
                bubble.ChangeSpeed(bubbleSlowFactor);
                IncrementMakeBubbleSlowAchievement(bubble);
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

    private void IncrementMakeBubbleSlowAchievement(BasicBubble slowedBubble)
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)slowedBubble.OwnerID.Value
            || !AchievementSaveSystem.instance || SceneManager.GetActiveScene().buildIndex == 5 || SceneManager.GetActiveScene().buildIndex == 6) return;

        AchievementSaveSystem achSaveSystem = AchievementSaveSystem.instance;
        achSaveSystem.IncrementStat(9, 1);
    }
}