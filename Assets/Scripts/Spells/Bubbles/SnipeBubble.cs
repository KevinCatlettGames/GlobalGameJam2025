using System.Collections;
using UnityEngine;
using Unity.Netcode;
using FMODUnity;

public class SnipeBubble : BasicBubble
{
    [SerializeField] private float minDamage = 10f;
    [SerializeField] private float damageRampUpDistance = 25f;

    private float damageScaling = 0f;
    private float maxDamage = 0f;
    private float currentDamage = 0f;

    public override void InitialiseBubble(int ID, Vector3 dir, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dir, playerCollider);

        maxDamage = damage;
        currentDamage = minDamage;

        // Prevent division by zero errors if the ramp-up distance is unassigned or set to zero
        damageScaling = damageRampUpDistance > 0f ? ((maxDamage - minDamage) / damageRampUpDistance) : 0f;
    }

    protected override void BubbleMovement()
    {
        // --- PREDICTION FILTER ---
        // Allow the local visual fake projectile to travel across the client screen
        if (!IsServer && !isLocalFake) return;

        base.BubbleMovement();

        if (currentDamage < maxDamage)
        {
            currentDamage += speed * Time.fixedDeltaTime * damageScaling;
            if (currentDamage > maxDamage) currentDamage = maxDamage;
        }
    }

    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped || other == null) return;
        if (!IsServer && !isLocalFake) return; // Process for authoritative server instances and client fakes

        // --- 1. SHARED COLLISION MODIFIER ---
        // Sniper projectiles destroy each other instantly on intersection
        if (other.CompareTag("Bubble"))
        {
            BasicBubble otherBubble = other.GetComponent<BasicBubble>();
            if (otherBubble != null && otherBubble.spellType == SpellType.Snipe)
            {
                Pop();
                return;
            }
        }

        // --- 3. AUTHORITATIVE SERVER LOGIC ---
        if (other.CompareTag("Player"))
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
        if (!IsServer && !isLocalFake) return; // Strict security layer backstop

        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)OwnerID.Value
            || !SteamIntegration.instance) return;

        SteamIntegration steamIntegration = SteamIntegration.instance;
        steamIntegration.IncrementIntSteamStat(
            steamIntegration.maxSniperDamageStatID,
            (int)maxDamage,
            steamIntegration.StatThresholds[steamIntegration.maxSniperDamageStatID],
            steamIntegration.maxRangeSniperDamageAchievementID
        );
    }
}