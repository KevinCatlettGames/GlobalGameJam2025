using UnityEngine;

public class SplitAchievementHandler : MonoBehaviour
{
    public SplitTracker tracker;
    private bool hasHit = false;

    private void OnCollisionEnter(Collision collision)
    {
        if (hasHit || tracker == null) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            hasHit = true;
            tracker.RegisterHit(collision.gameObject);
        }
    }
}

public class SplitTracker
{
    public GameObject firstHitPlayer = null;
    public bool achievementUnlocked = false;

    public void RegisterHit(GameObject playerHit)
    {
        if (achievementUnlocked) return;

        if (firstHitPlayer == null)
        {
            firstHitPlayer = playerHit;
        }
        else if (firstHitPlayer != playerHit)
        {
            achievementUnlocked = true;
            IncrementSplitAchievement();
        }
    }

    private void IncrementSplitAchievement()
    {
        if (!AchievementSaveSystem.instance) return;
        AchievementSaveSystem.instance.IncrementStat(11, 1);
    }
}