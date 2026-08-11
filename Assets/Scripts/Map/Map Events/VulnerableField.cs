using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class VulnerableField : MonoBehaviour
{
    [SerializeField] private float duration = 0.5f;
    private List<PlayerController> playersInRange = new List<PlayerController>();
    private bool isActive = false;

    private void FixedUpdate()
    {
        foreach (PlayerController player in playersInRange)
        {
            player.StartVulnerable(duration);
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playersInRange.Add(other.GetComponent<PlayerController>());

            if(!other.GetComponent<PlayerController>().IsVulnerable)
                IncrementProcEffectAchievement();
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && playersInRange.Contains(other.GetComponent<PlayerController>()))
        {
            playersInRange.Remove(other.GetComponent<PlayerController>());
        }
    }

    private void IncrementProcEffectAchievement()
    {
        if (SceneManager.GetActiveScene().buildIndex == 5 || SceneManager.GetActiveScene().buildIndex == 6) return;

        if (AchievementSaveSystem.instance)
        {
            AchievementSaveSystem.instance.IncrementStat(20, 1);
        }
    }
}
