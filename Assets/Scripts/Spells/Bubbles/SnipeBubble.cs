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

        if (playerCollider != null)
        {
            Physics.IgnoreCollision(GetComponent<Collider>(), playerCollider, true);
            StartCoroutine(ReenableCollisionAfterDelay(1f));
        }
    }

    private IEnumerator ReenableCollisionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (playerCollider != null)
        {
            Physics.IgnoreCollision(GetComponent<Collider>(), playerCollider, false);
        }
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
            PlayerController player = other.GetComponent<PlayerController>();
            GameManager gameManager = GameManager.Instance;
            
            if (gameManager.PlayingLocal)
                player.ApplyKnockbackLocal(OwnerID, direction, knockback, currentDamage);
            else
                player.ApplyKnockbackServerRpc(OwnerID, direction, knockback, currentDamage);
            
            gameManager.ChangeHitReference(OwnerID, spellType, player.PlayerID, isSoaped, isReflected);
            
            if (currentDamage >= maxDamage)
                CheckMaxSniperDamageAchievement();
            
            Pop();
        }
        else if (other.CompareTag("Bubble"))
        {
            if (other.TryGetComponent<SnipeBubble>(out SnipeBubble snipeComponent))
            {
                snipeComponent.Pop();
                Pop();
            }
        }
        else
        {
            Pop();
        }
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