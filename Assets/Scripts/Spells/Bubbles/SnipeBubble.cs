using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SnipeBubble : BasicBubble
{
    [SerializeField] private float minDamage = 10f;
    [SerializeField] private float damageRampUpDistance = 25f;

    private float damageScaling = 0f;
    private float maxDamage = 0f;
    private float currentDamage = 0f;

    public override void InitialiseBubble(int ID, Vector3 dir, Collider playerCollider, int assignedSpellID, bool fakeWithServerCaster)
    {
        base.InitialiseBubble(ID, dir, playerCollider, assignedSpellID, fakeWithServerCaster);

        maxDamage = damage;
        currentDamage = minDamage;

        damageScaling = damageRampUpDistance > 0f ? ((maxDamage - minDamage) / damageRampUpDistance) : 0f;
    }

    protected override void BubbleMovement()
    {
        if (!IsServer && !isLocalFake) return;

        base.BubbleMovement();

        // Dynamically scale damage based on movement distance
        if (currentDamage < maxDamage)
        {
            currentDamage += speed * Time.fixedDeltaTime * damageScaling;
            if (currentDamage > maxDamage) currentDamage = maxDamage;
        }
    }

    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped || other == null) return;
        if (!IsServer && !isLocalFake) return;

        // Snipe vs Snipe collision handling
        if (other.CompareTag("Bubble"))
        {
            BasicBubble otherBubble = other.GetComponent<BasicBubble>();
            if (otherBubble != null && otherBubble.spellType == SpellType.Snipe)
            {
                Pop();
                return;
            }
        }

        // Local predicted fake collision handling
        if (isLocalFake)
        {
            damage = currentDamage;
            Pop();
            return;
        }

        // Authoritative server hit evaluation
        if (IsServer && other.CompareTag("Player"))
        {
            if (currentDamage >= maxDamage)
            {
                CheckMaxSniperDamageAchievement();
            }
        }

        damage = currentDamage;
        base.BubbleCollision(other);
    }

    private void CheckMaxSniperDamageAchievement()
    {
        if (!IsServer) return;

        if ((TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)OwnerID.Value)
            || !AchievementSaveSystem.instance || SceneManager.GetActiveScene().buildIndex == 5 || SceneManager.GetActiveScene().buildIndex == 6) return;

        AchievementSaveSystem achSaveSystem = AchievementSaveSystem.instance;
        achSaveSystem.IncrementStat(1, (int)maxDamage);
        achSaveSystem.IncrementStat(24, (int)maxDamage);
    }
}