using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GrenadeBubble : BasicBubble
{
    private bool hasExploded = false;
    [Header("Special Stats")]
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float primaryTargetMod = 2f;
    [SerializeField] private float vulnerableDuration = 4f;
    [SerializeField] private AnimationCurve arc;
    [SerializeField] private GameObject splat;
    [SerializeField] private GameObject fakeSplat; 

    [SerializeField] private LayerMask groundedLayerMask;
    private float progress = 0f;
    private const float raycastDistance = 5f;
    private GameObject primaryTarget = null;

    public override void InitialiseBubble(int ID, Vector3 dir, Collider playerCollider, int assignedSpellID, bool fakeWithServerCaster)
    {
        base.InitialiseBubble(ID, dir, playerCollider, assignedSpellID, fakeWithServerCaster);
        canMiss = false;
    }

    protected override void BubbleMovement()
    {
        if (!IsServer && !isLocalFake) return;

        progress += speed * Time.fixedDeltaTime;

        float evaluationPoint = range > 0f ? (progress / range) : 0f;
        transform.position = new Vector3(transform.position.x, arc.Evaluate(evaluationPoint), transform.position.z);

        base.BubbleMovement();

        if (transform.position.y <= 0.1f)
        {
            transform.position = new Vector3(transform.position.x, 0.1f, transform.position.z);
            Pop();
        }
    }

    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped) return;
        if (!IsServer && !isLocalFake) return;

        if (isLocalFake)
        {
            fizzleEffect = hitEffect;         
            Pop();
            return;
        }

        if (IsServer)
        {
            fizzleEffect = hitEffect;
            ChangeHitEffectClientRpc();
        }

        if (other != null && other.CompareTag("Player"))
        {
            primaryTarget = other;
            IncrementPerfectGrenadeHitAchievement();
        }

        if(other != null && other.CompareTag("Bubble") && other.GetComponent<GrenadeBubble>() && other.GetComponent<GrenadeBubble>().OwnerID.Value != OwnerID.Value)
        {
            UnlockHitTwoGrenadesMidairAchievement();
        }

        fizzleEffect = hitEffect;
        if (IsServer)
            ChangeHitEffectClientRpc();
        Pop();
    }

    protected override void Pop()
    {
        if (hasInflated)
            Explode();
        base.Pop();
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        fizzleEffect = hitEffect;
        if (IsServer)
            ChangeHitEffectClientRpc();

        Collider[] explosionOverlaps = Physics.OverlapSphere(transform.position, explosionRadius, LayerMask.GetMask("Bubble", "Player"));
        Vector3 origin;
        Vector3 direction;
        foreach (Collider col in explosionOverlaps)
        {
            if (!col || col.gameObject == gameObject) continue;
            origin = transform.position;
            direction = col.transform.position - transform.position;
            if (!Physics.Raycast(origin, direction, direction.magnitude, LayerMask.GetMask("Wall")))
            {
                if (col.CompareTag("Player"))
                {
                    if (IsServer)
                    {
                        GameManager gameManager = GameManager.Instance;
                        PlayerController player = col.GetComponent<PlayerController>();
                        if (player != null)
                        {
                            float explosionDamage = damage;
                            float explosionKnockback = knockback;
                            if (player.gameObject == primaryTarget)
                            {
                                explosionDamage *= primaryTargetMod;
                                explosionKnockback *= primaryTargetMod;
                            }
                            if (gameManager.PlayingLocal)
                                player.ApplyKnockbackLocal(OwnerID.Value, direction, explosionKnockback, explosionDamage);
                            else
                                player.ApplyKnockbackServerRpc(OwnerID.Value, direction, explosionKnockback, explosionDamage);

                            gameManager.ChangeHitReference(OwnerID.Value, spellType, player.PlayerID, isSoaped, isReflected, false);
                            player.StartVulnerable(vulnerableDuration);

                            if (playerCollider != null)
                            {
                                var controller = playerCollider.GetComponent<PlayerController>();
                                if (controller != null) controller.GainUltCharge(explosionDamage, true);
                                if (controller != null) GameManager.Instance.ChangeHitReference(OwnerID.Value, spellType, player.PlayerID, false, false, true);

                            }
                        }
                    }
                }
                else
                {
                    BasicBubble bubble = col.GetComponent<BasicBubble>();
                    if (bubble != null)
                    {
                        bubble.BubbleCollision(gameObject);
                    }
                }
            }
        }

        if (Physics.Raycast(new Vector3(transform.position.x, 2f, transform.position.z), Vector3.down, out RaycastHit hitInfo, raycastDistance, groundedLayerMask))
        {
            if (IsServer)
            {
                GameObject puddle = Instantiate(splat, hitInfo.point, transform.rotation);
                puddle.GetComponent<NetworkObject>()?.Spawn();
                puddle.GetComponent<DamageField>()?.SetID(OwnerID.Value);
                puddle.GetComponent<Puddle>().InitialisePuddle(playerCollider);
            }
            else if(isLocalFake)
            {
                GameObject puddle = Instantiate(fakeSplat, hitInfo.point, transform.rotation);
                puddle.GetComponent<Puddle>().isLocalFake = true;
            }
        }
    }

    protected override void Reflect(Vector3 normal)
    {
        progress = 0;
        base.Reflect(normal);
    }

    [ClientRpc]
    void ChangeHitEffectClientRpc()
    {
        if (IsServer) return;
        fizzleEffect = hitEffect;
    }

    private void IncrementPerfectGrenadeHitAchievement()
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)OwnerID.Value 
            || !AchievementSaveSystem.instance || SceneManager.GetActiveScene().buildIndex == 5 || SceneManager.GetActiveScene().buildIndex == 6) return;

        AchievementSaveSystem.instance.IncrementStat(8);
    }

    private void UnlockHitTwoGrenadesMidairAchievement()
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)OwnerID.Value 
            || !AchievementSaveSystem.instance || SceneManager.GetActiveScene().buildIndex == 5 || SceneManager.GetActiveScene().buildIndex == 6) return;

        AchievementSaveSystem.instance.UnlockAchievement(18);
    }
}