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

        // Advance progress on active movement
        progress += speed * Time.fixedDeltaTime;

        float evaluationPoint = range > 0f ? Mathf.Clamp01(progress / range) : 0f;
        transform.position = new Vector3(transform.position.x, arc.Evaluate(evaluationPoint), transform.position.z);

        base.BubbleMovement();

        // Floor impact detection
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

        if (other != null && other.CompareTag("Bubble"))
        {
            GrenadeBubble otherGrenade = other.GetComponent<GrenadeBubble>();
            if (otherGrenade != null && otherGrenade.OwnerID.Value != OwnerID.Value)
            {
                UnlockHitTwoGrenadesMidairAchievement();
            }
        }

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
        {
            ChangeHitEffectClientRpc();
        }

        // --- Server Damage & Knockback Area Evaluation ---
        if (IsServer)
        {
            Collider[] explosionOverlaps = Physics.OverlapSphere(transform.position, explosionRadius, LayerMask.GetMask("Bubble", "Player"));

            foreach (Collider col in explosionOverlaps)
            {
                if (!col || col.gameObject == gameObject) continue;

                Vector3 origin = transform.position;
                Vector3 targetDir = col.transform.position - transform.position;

                if (!Physics.Raycast(origin, targetDir, targetDir.magnitude, LayerMask.GetMask("Wall")))
                {
                    if (col.CompareTag("Player"))
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
                                player.ApplyKnockbackLocal(OwnerID.Value, targetDir.normalized, explosionKnockback, explosionDamage);
                            else
                                player.ApplyKnockbackServerRpc(OwnerID.Value, targetDir.normalized, explosionKnockback, explosionDamage);

                            gameManager.ChangeHitReference(OwnerID.Value, spellType, player.PlayerID, isSoaped, isReflected, false);
                            player.StartVulnerable(vulnerableDuration);

                            if (playerCollider != null)
                            {
                                var controller = playerCollider.GetComponent<PlayerController>();
                                if (controller != null)
                                {
                                    controller.GainUltCharge(explosionDamage, true);
                                    gameManager.ChangeHitReference(OwnerID.Value, spellType, player.PlayerID, false, false, true);
                                }
                            }
                        }
                    }
                    else if (col.CompareTag("Bubble"))
                    {
                        BasicBubble bubble = col.GetComponent<BasicBubble>();
                        if (bubble != null)
                        {
                            bubble.BubbleCollision(gameObject);
                        }
                    }
                }
            }
        }

        // --- Ground Splat Spawning ---
        if (Physics.Raycast(new Vector3(transform.position.x, 2f, transform.position.z), Vector3.down, out RaycastHit hitInfo, raycastDistance, groundedLayerMask))
        {
            if (IsServer)
            {
                GameObject puddle = Instantiate(splat, hitInfo.point, transform.rotation);
                puddle.GetComponent<NetworkObject>()?.Spawn();
                puddle.GetComponent<DamageField>()?.SetID(OwnerID.Value);
                puddle.GetComponent<Puddle>()?.InitialisePuddle(playerCollider);
            }
            else if (isLocalFake)
            {
                GameObject puddle = Instantiate(fakeSplat, hitInfo.point, transform.rotation);
                Puddle pScript = puddle.GetComponent<Puddle>();
                if (pScript != null) pScript.isLocalFake = true;
            }
        }
    }

    protected override void Reflect(Vector3 normal)
    {
        progress = 0f;
        base.Reflect(normal);
    }

    protected override bool DetectsImpact(out Vector3 impactPoint)
    {
        impactPoint = transform.position;

        // Calculate actual trajectory vector including vertical arc
        float currentEval = range > 0f ? Mathf.Clamp01(progress / range) : 0f;
        float nextEval = range > 0f ? Mathf.Clamp01((progress + (speed * Time.fixedDeltaTime)) / range) : 0f;

        Vector3 currentPos = transform.position;
        Vector3 nextPos = transform.position + (direction * speed * Time.fixedDeltaTime);
        nextPos.y = arc.Evaluate(nextEval);

        Vector3 moveDelta = nextPos - currentPos;
        float moveDistance = moveDelta.magnitude;

        if (moveDistance <= 0.001f) return false;

        float checkRadius = (sphereCollider != null) ? (sphereCollider.radius * transform.localScale.x * 0.5f) : (currentSize * 0.5f);

        if (Physics.SphereCast(currentPos, checkRadius, moveDelta.normalized, out RaycastHit hit, moveDistance + 0.05f))
        {
            if (ignoredColliders.Contains(hit.collider) || hit.collider.transform.root == transform.root)
                return false;

            impactPoint = hit.point;
            return true;
        }

        return false;
    }

    [ClientRpc]
    private void ChangeHitEffectClientRpc()
    {
        if (IsServer || isLocalFake) return;
        fizzleEffect = hitEffect;
    }

    private void IncrementPerfectGrenadeHitAchievement()
    {
        if ((TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)OwnerID.Value)
            || !AchievementSaveSystem.instance || SceneManager.GetActiveScene().buildIndex == 5 || SceneManager.GetActiveScene().buildIndex == 6) return;

        AchievementSaveSystem.instance.IncrementStat(8);
    }

    private void UnlockHitTwoGrenadesMidairAchievement()
    {
        if ((TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)OwnerID.Value)
            || !AchievementSaveSystem.instance || SceneManager.GetActiveScene().buildIndex == 5 || SceneManager.GetActiveScene().buildIndex == 6) return;

        AchievementSaveSystem.instance.UnlockAchievement(18);
    }
}