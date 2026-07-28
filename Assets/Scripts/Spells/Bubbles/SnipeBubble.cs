using UnityEngine;
using Unity.Netcode;

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

        if (other.CompareTag("Bubble"))
        {
            BasicBubble otherBubble = other.GetComponent<BasicBubble>();
            if (otherBubble != null && otherBubble.spellType == SpellType.Snipe)
            {
                Pop();
                return;
            }
        }
     
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
        if (!IsServer && !isLocalFake) return;

        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)OwnerID.Value
            || !SteamIntegration.instance) return;

        SteamIntegration steamIntegration = SteamIntegration.instance;
        steamIntegration.IncrementIntSteamStat(1, (int)maxDamage);
        steamIntegration.IncrementIntSteamStat(24, (int)maxDamage);
    }
}