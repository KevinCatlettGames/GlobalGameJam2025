using FMODUnity;
using System.Collections;
using UnityEngine;
using Unity.Netcode; 

public class SnipeBubble : BasicBubble
{
    [SerializeField] private float minDamage = 10f;
    [SerializeField] private float damageRampUpDistance = 25f;

    private float damageScaling = 0f;
    private float maxDamage = 0f;
    private float currentDamage = 0f;
    
    public override void InitialiseBubble(int ID, float dmg, float knb, float spd, float rng, float siz, float inf, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dmg, knb, spd, rng, siz, inf, dir, soundEvent, playerCollider);

        maxDamage = dmg;
        currentDamage = minDamage;
        damageScaling = (maxDamage - minDamage) / damageRampUpDistance;
    }

    protected override void BubbleMovement()
    {
        base.BubbleMovement();

        if (currentDamage < maxDamage)
        {
            currentDamage += speed * Time.fixedDeltaTime * damageScaling;
            if (currentDamage > maxDamage) currentDamage = maxDamage;
        }
    }

    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped || !IsServer) return;

        if (other.CompareTag("Player"))
        {
            if (currentDamage >= maxDamage)
                CheckMaxSniperDamageAchievement();
        }
        else if (other.CompareTag("Bubble"))
        {
            if (other.TryGetComponent<SnipeBubble>(out SnipeBubble snipeComponent))
            {
                snipeComponent.Pop();
                Pop();
                return;
            }
        }
        damage = currentDamage;
        base.BubbleCollision(other);
    }

    void CheckMaxSniperDamageAchievement()
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)OwnerID 
            || !SteamIntegration.instance) return;
        
        SteamIntegration steamIntegration = SteamIntegration.instance;
        steamIntegration.IncrementIntSteamStat(steamIntegration.maxSniperDamageStatID, 
            (int)maxDamage,
            steamIntegration.StatThresholds[steamIntegration.maxSniperDamageStatID], 
            steamIntegration.maxRangeSniperDamageAchievementID);
    }
}