using System.Collections.Generic;
using UnityEngine;

public class WindField : MonoBehaviour
{
    [SerializeField] private float windStrength = 0.5f;
    private List<PlayerController> playersInRange = new List<PlayerController>();

    private void FixedUpdate()
    {
        foreach (PlayerController player in playersInRange)
        {
            if (player != null)
            {
                player.ApplyImpulseLocal(transform.forward, windStrength * Time.fixedDeltaTime);
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playersInRange.Add(other.GetComponent<PlayerController>());
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && playersInRange.Contains(other.GetComponent<PlayerController>()))
        {
            playersInRange.Remove(other.GetComponent<PlayerController>());
        }
    }
}
