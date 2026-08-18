using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ExplodingBubble : BasicBubble
{
    [Header("Special Stats")]
    [SerializeField] private bool indicator = true;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private GameObject earlyFizzleEffect;
    [SerializeField] private float primaryKnockbackIncrease = 1.2f;
    private bool isReadyToExpode = false;
    private bool hasExploded = false;
    private GameObject primaryTarget;
    private bool wasDetonatedByBubble = false;

    public override void InitialiseBubble(int ID, Vector3 dir, Collider playerCollider, int assignedSpellID, bool fakeWithServerCaster)
    {
        base.InitialiseBubble(ID, dir, playerCollider, assignedSpellID, fakeWithServerCaster);
        canMiss = false;
    }

    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped) return;
        if (!IsServer && !isLocalFake) return;

        if (other != null && other.CompareTag("Bubble") && popOnBubbleHit)
        {
            var otherBubble = other.GetComponent<BasicBubble>();
            if (otherBubble != null)
            {
                if (otherBubble.OwnerID.Value == OwnerID.Value)
                {
                    wasDetonatedByBubble = true;
                }
                OwnerID = otherBubble.OwnerID;
            }
        }
        else if (other != null && other.CompareTag("Player"))
        {
            primaryTarget = other;
        }

        fizzleEffect = hitEffect;
        Pop();
    }

    protected override void InflateOverlapChack()
    {
        isReadyToExpode = true;
        base.InflateOverlapChack();
    }

    public void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        if (isLocalFake) return;

        Collider[] explosionOverlaps = Physics.OverlapSphere(transform.position, explosionRadius, LayerMask.GetMask("Bubble", "Player"));
        Vector3 origin;
        Vector3 direction;
        foreach (Collider col in explosionOverlaps)
        {
            if (col == null || col.gameObject == this.gameObject) continue;
            origin = transform.position;
            direction = col.transform.position - transform.position;
            if (!Physics.Raycast(origin, direction, direction.magnitude, LayerMask.GetMask("Wall")))
            {
                if (col.CompareTag("Player"))
                {
                    PlayerController player = col.GetComponent<PlayerController>();
                    if (player != null)
                    {
                        if (col.gameObject == primaryTarget)
                            knockback *= primaryKnockbackIncrease;

                        if (GameManager.Instance.PlayingLocal)
                            player.ApplyKnockbackLocal(OwnerID.Value, direction, knockback, damage, isCrit);
                        else
                            player.ApplyKnockbackServerRpc(OwnerID.Value, direction, knockback, damage, isCrit);

                        if (playerCollider != null)
                        {
                            var controller = playerCollider.GetComponent<PlayerController>();
                            if (controller != null) controller.GainUltCharge(damage, true);
                            {
                                controller.GainUltCharge(damage, true);
                                GameManager.Instance.ChangeHitReference(OwnerID.Value, spellType, player.PlayerID, false, false, true);
                                GameManager.Instance.RegisterExplosionHit(
                                    OwnerID.Value,
                                    player.PlayerID,
                                    AssignedSpellID.Value,
                                    wasDetonatedByBubble
                                );
                            }
                        }

                        if (col.gameObject == primaryTarget)
                            knockback /= primaryKnockbackIncrease;
                    }
                }
                else
                {
                    BasicBubble bubble = col.GetComponent<BasicBubble>();
                    if (bubble != null)
                    {
                        bubble.BubbleCollision(this.gameObject);
                    }
                }
            }
        }
    }

    public void OnExplosionKilledPlayer()
    {
        if (!wasDetonatedByBubble) return;

        UnlockDetonationMultiKillAchievement(OwnerID.Value);
    }
    private void UnlockDetonationMultiKillAchievement(int killerID)
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)killerID
            || !AchievementSaveSystem.instance || SceneManager.GetActiveScene().buildIndex == 5) return;

        AchievementSaveSystem.instance.IncrementStat(5, 1);
    }

    protected override void Pop()
    {
        if (hasPopped) return;

        if (isReadyToExpode)
        {
            Explode();
            fizzleEffect = hitEffect;
        }
        else
        {
            fizzleEffect = earlyFizzleEffect;
        }
        base.Pop();
    }

    public void ChangeToEarlyFizzle()
    {
        fizzleEffect = earlyFizzleEffect;
    }
}