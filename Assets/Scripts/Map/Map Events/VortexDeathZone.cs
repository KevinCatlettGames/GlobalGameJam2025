using System.Collections.Generic;
using UnityEngine;

public class VortexDeathZone : MonoBehaviour
{
    private List<PlayerController> playersInRange = new List<PlayerController>();
    private float[] timeInZone = new float[4];
    [SerializeField] private float timeToDeath = 1f;

    private void FixedUpdate()
    {
        for (int i = playersInRange.Count -1; i >= 0; i--)
        {
            int id = playersInRange[i].PlayerID;
            timeInZone[id] += Time.fixedDeltaTime;
            if (timeInZone[id] >= timeToDeath)
            {
                timeInZone[id] = 0f;
                playersInRange[i].GetComponent<PlayerStateHandler>().KillPlayer();
                playersInRange.RemoveAt(i);
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            player.SetDoomed(true);
            playersInRange.Add(player);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && playersInRange.Contains(other.GetComponent<PlayerController>()))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            player.SetDoomed(false);
            timeInZone[player.PlayerID] = 0f;
            playersInRange.Remove(player);
        }
    }
}
