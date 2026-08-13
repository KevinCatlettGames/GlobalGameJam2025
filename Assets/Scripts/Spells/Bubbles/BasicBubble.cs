using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BasicBubble : NetworkBehaviour
{
    public enum SpellType
    {
        Null, Basic, Exploding, Giant, SmallerGiant, Homing, Revolver, Snipe, Soap,
        Wall, Grenade, Demolish, Ink, Boomerang, Blast, Harpoon, Slasher, Zap,
        Teleport, Cross, Split
    };

    public enum FakeBubbleState
    {
        Moving,
        AwaitingServerConfirmation,
        Popped
    }

    public SpellType spellType;
    public NetworkVariable<int> OwnerID = new NetworkVariable<int>();
    public NetworkVariable<int> AssignedSpellID = new NetworkVariable<int>();
    public BasicBubble fakeCopy = null;
    public bool fakeWithServerCaster = false;
    protected Vector3 direction;
    protected bool hasPopped;
    [HideInInspector] public bool HasPopped { get { return hasPopped; } }

    [Header("Bubble Base Stats")]
    [SerializeField] protected float size = 1f;
    [SerializeField] protected float damage = 1.0f;
    [SerializeField] protected float knockback = 1.0f;
    [SerializeField] protected float speed = 1.0f;
    public float Speed => speed;
    [SerializeField] protected float range = 1.0f;
    [SerializeField] protected float inflationSpeed = 8f;

    protected Coroutine rangeCoroutine;
    protected SphereCollider sphereCollider;
    protected float currentSize = 0.01f;
    protected Collider playerCollider;
    protected List<Collider> ignoredColliders = new List<Collider>();
    protected bool isSoaped = false;
    public bool IsSoaped { get { return isSoaped; } }
    protected bool isReflected = false;
    protected bool hasInflated = false;

    [Header("Hit behaviour")]
    [SerializeField] protected bool popOnPlayerHit = true;
    [SerializeField] protected bool popOnBubbleHit = true;

    [Header("Effects")]
    [SerializeField] protected GameObject fizzleEffect;
    [SerializeField] protected GameObject hitEffect;
    [SerializeField] protected EventReference soundEvent;
    private float soapSpeedAmp = 2f;
    private float soapSecSpeedAmp = .5f;
    private float soapSecSpeedIncrease = 0f;
    private float reflectDmgIncrease = 1.2f;

    protected Vector3 lastPosition;
    protected float desyncThreshold = 0.05f;

    protected bool canMiss = true;
    protected bool isUlt = false;
    protected bool hasHitPlayer = false;

    public bool isLocalFake = false;

    [Header("Client-Side Prediction State")]
    private FakeBubbleState currentState = FakeBubbleState.Moving;
    private Vector3 savedVelocity;
    private float pendingTimer = 0f;
    private const float MAX_WAIT_TIME = 0.15f; // 150ms timeout safety net
    private Coroutine correctionCoroutine;

    private void Start()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameEnded += DestroyBubble;
    }

    public virtual void InitialiseBubble(int ID, Vector3 dir, Collider playerCollider, int assignedSpellID, bool serverSpawnWithClientSpawn)
    {
        OwnerID.Value = ID;
        AssignedSpellID.Value = assignedSpellID;
        this.fakeWithServerCaster = serverSpawnWithClientSpawn;
        direction = dir;
        this.playerCollider = playerCollider;

        rangeCoroutine = StartCoroutine(BubbleRangeLimit());

        RuntimeManager.PlayOneShotAttached(soundEvent, gameObject);

        sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider != null)
        {
            // Disable trigger interactions on fake to prevent premature local physics overrides
            if (isLocalFake)
            {
                sphereCollider.isTrigger = false;
            }

            List<PlayerController> team = GameManager.Instance.GetTeam(ID);
            if (team != null)
            {
                foreach (PlayerController player in team)
                {
                    if (player != null)
                        ignoredColliders.Add(player.Controller);
                }
            }
            else
            {
                if (playerCollider != null)
                    ignoredColliders.Add(playerCollider);
            }
            foreach (Collider col in ignoredColliders)
            {
                Physics.IgnoreCollision(sphereCollider, col);
            }

            StartCoroutine(Inflate());
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
            RuntimeManager.PlayOneShotAttached(soundEvent, gameObject);

        OwnerID.OnValueChanged += OnOwnerIdAssigned;
        AssignedSpellID.OnValueChanged += OnSpellIdAssigned;

        if (!IsServer && !isLocalFake)
        {
            if (AssignedSpellID.Value != 0)
            {
                TryLinkLocalFake(AssignedSpellID.Value);
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        OwnerID.OnValueChanged -= OnOwnerIdAssigned;
        AssignedSpellID.OnValueChanged -= OnSpellIdAssigned;
    }

    private void OnSpellIdAssigned(int previousValue, int newValue)
    {
        if (!IsServer && !isLocalFake && newValue != 0)
        {
            TryLinkLocalFake(newValue);
        }
    }

    private void TryLinkLocalFake(int targetSpellId)
    {
        if (fakeCopy != null) return;

        BasicBubble[] allBubbles = FindObjectsByType<BasicBubble>(FindObjectsSortMode.None);

        foreach (var bubble in allBubbles)
        {
            if (bubble.isLocalFake && bubble.AssignedSpellID.Value == targetSpellId)
            {
                fakeCopy = bubble;

                Collider myCollider = GetComponent<Collider>();
                Collider fakeCollider = bubble.GetComponent<Collider>();
                if (myCollider != null && fakeCollider != null)
                {
                    Physics.IgnoreCollision(myCollider, fakeCollider, true);
                }
                break;
            }
        }
    }

    void OnOwnerIdAssigned(int previousValue, int newValue)
    {
        CheckAndHideVisibility(newValue);
    }

    void CheckAndHideVisibility(int currentCasterId)
    {
        if (IsServer || isLocalFake) return;

        if (currentCasterId < 0 || currentCasterId >= GameManager.Instance.Players.Length) return;
        if (GameManager.Instance.Players[currentCasterId] == null) return;

        if (GameManager.Instance.Players[currentCasterId].IsOwner || fakeWithServerCaster)
        {
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
                renderer.enabled = false;

            foreach (ParticleSystem ps in GetComponentsInChildren<ParticleSystem>(true))
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ps.gameObject.SetActive(false);
            }

            foreach (TrailRenderer trail in GetComponentsInChildren<TrailRenderer>(true))
            {
                trail.Clear();
                trail.enabled = false;
            }

            Collider bubbleCol = GetComponent<Collider>();
            Collider playerCol = GameManager.Instance.Players[OwnerID.Value].GetComponent<Collider>();
            if (bubbleCol != null && playerCol != null)
            {
                Physics.IgnoreCollision(bubbleCol, playerCol);
            }
        }
    }

    private void FixedUpdate()
    {
        if (isLocalFake)
        {
            switch (currentState)
            {
                case FakeBubbleState.Moving:
                    BubbleMovement();
                    break;

                case FakeBubbleState.AwaitingServerConfirmation:
                    // GLIDE FORWARD at 20% speed while awaiting server approval
                    // This converts a jarring "lag freeze" into a smooth impact glide/squish
                    transform.position += direction * (speed * 0.2f) * Time.fixedDeltaTime;

                    pendingTimer += Time.fixedDeltaTime;

                    // Safety Timeout: Resume normal speed if server drops packet or takes too long
                    if (pendingTimer >= MAX_WAIT_TIME)
                    {
                        currentState = FakeBubbleState.Moving;
                    }
                    break;

                case FakeBubbleState.Popped:
                    break;
            }
        }
        else if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            BubbleMovement();
        }
    }

    protected virtual void CheckLocalCollisions()
    {
        if (!isLocalFake || currentState != FakeBubbleState.Moving) return;

        if (DetectsImpact(out Vector3 impactPoint))
        {
            savedVelocity = direction * speed;
            currentState = FakeBubbleState.AwaitingServerConfirmation;
            pendingTimer = 0f;

            NotifyServerImpactServerRpc(impactPoint);
        }
    }

    protected virtual bool DetectsImpact(out Vector3 impactPoint)
    {
        impactPoint = transform.position;

        // Check ONLY the exact distance the bubble moves in 1 physics frame (plus a tiny safety buffer)
        float lookAheadDistance = (speed * Time.fixedDeltaTime) + 0.05f;

        // Radius uses half size so it doesn't trigger on surfaces further away
        float checkRadius = (sphereCollider != null) ? (sphereCollider.radius * transform.localScale.x * 0.5f) : (currentSize * 0.5f);

        if (Physics.SphereCast(transform.position, checkRadius, direction, out RaycastHit hit, lookAheadDistance))
        {
            if (ignoredColliders.Contains(hit.collider) || hit.collider.transform.root == transform.root)
                return false;

            impactPoint = hit.point;
            return true;
        }
        return false;
    }

    [ServerRpc(RequireOwnership = false)]
    protected void NotifyServerImpactServerRpc(Vector3 clientImpactPoint)
    {
        // Server verification hook for predicted client hit/impact
    }

    protected virtual IEnumerator Inflate()
    {
        if (sphereCollider != null) sphereCollider.excludeLayers += LayerMask.GetMask("Player");

        while (currentSize < size)
        {
            currentSize += inflationSpeed * Time.deltaTime;
            if (currentSize > size) currentSize = size;
            transform.localScale = Vector3.one * currentSize;
            yield return null;
        }

        if (IsServer)
        {
            InflateOverlapChack();
        }

        if (sphereCollider != null) sphereCollider.excludeLayers -= LayerMask.GetMask("Player");
        hasInflated = true;
    }

    protected virtual void InflateOverlapChack()
    {
        Collider[] overlaps = Physics.OverlapSphere(transform.position, size, LayerMask.GetMask("Player"));
        foreach (Collider col in overlaps)
        {
            if (ignoredColliders.Contains(col)) continue;
            BubbleCollision(col.gameObject);
            break;
        }
    }

    protected virtual void BubbleMovement()
    {
        Vector3 nextPosition = transform.position + (direction * speed * Time.fixedDeltaTime);
        transform.position = nextPosition;

        if (isLocalFake)
        {
            CheckLocalCollisions();
        }
    }

    protected virtual IEnumerator BubbleRangeLimit()
    {
        float lifetime = range / speed;
        yield return new WaitForSeconds(lifetime);

        if (IsServer)
        {
            if (canMiss)
            {
                IncrementMissedShotAchievement();
                GameManager.Instance.OnWeaponMissed(OwnerID.Value);
            }

            Pop();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasPopped || isLocalFake || !IsServer) return;
        if (other.transform.root == transform.root) return;
        HandleTrigger(other);
    }

    public virtual void HandleTrigger(Collider other)
    {
        if (other.gameObject.TryGetComponent<Reflector>(out var reflector) && reflector.GetIsReflecting())
        {
            OwnerID.Value = reflector.OwnerID;

            Collider myCollider = GetComponent<Collider>();
            Vector3 reflectNormal = Vector3.up;

            bool hasOverlap = Physics.ComputePenetration(
                myCollider, transform.position, transform.rotation,
                other, other.transform.position, other.transform.rotation,
                out Vector3 dir, out float distance
            );

            if (hasOverlap)
                reflectNormal = dir;
            else
                reflectNormal = (transform.position - other.transform.position).normalized;

            Reflect(reflectNormal);
            return;
        }

        if (other.CompareTag("Bubble") && other.GetComponent<NetworkObject>().IsSpawned || other.CompareTag("Puddle"))
            return;

        BubbleCollision(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasPopped || isLocalFake || !IsServer) return;
        HandleCollision(collision);
    }

    private void HandleCollision(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<Reflector>(out var reflector) && reflector.GetIsReflecting())
        {
            OwnerID.Value = reflector.OwnerID;
            Vector3 reflectNormal = collision.GetContact(0).normal;
            Reflect(reflectNormal);
            return;
        }

        BubbleCollision(collision.gameObject);
    }

    public virtual void BubbleCollision(GameObject other)
    {
        if (hasPopped || !IsServer) return;

        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<PlayerController>();
            GameManager gameManager = GameManager.Instance;
            if (gameManager.PlayingLocal)
                player.ApplyKnockbackLocal(OwnerID.Value, direction, knockback, damage);
            else
            {
                if (IsOwner)
                    player.ApplyKnockbackServerRpc(OwnerID.Value, direction, knockback, damage);
            }
            gameManager.ChangeHitReference(OwnerID.Value, spellType, player.PlayerID, isSoaped, isReflected, false, AssignedSpellID.Value);
            if (!isUlt) playerCollider.GetComponent<PlayerController>().GainUltCharge(damage, true);

            fizzleEffect = hitEffect;
            hasHitPlayer = true;
            if (popOnPlayerHit)
            {
                Pop();
            }
        }
        else if (other.CompareTag("Bubble"))
        {
            if (popOnBubbleHit)
            {
                Pop();
            }
        }
        else if (other.CompareTag("Puddle"))
        {
            return;
        }
        else
        {
            Pop();
        }
    }

    protected virtual void Pop()
    {
        if (hasPopped) return;

        hasPopped = true;
        currentState = FakeBubbleState.Popped;
        StopAllCoroutines();

        if (IsServer)
        {
            MakeFakePopAswellClientRpc(transform.position, hasHitPlayer);
            SpawnPopEffectClientRpc(transform.position);
            NetworkObject.Despawn(true);
            Destroy(gameObject);
        }
        else if (isLocalFake)
        {
            if (fizzleEffect != null)
                Instantiate(fizzleEffect, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }

    public void OnServerConfirmPop()
    {
        currentState = FakeBubbleState.Popped;
        Pop();
    }

    public void OnServerConfirmReflect(Vector3 newDirection, Vector3 correctPosition)
    {
        transform.position = Vector3.Lerp(transform.position, correctPosition, 0.5f);
        direction = newDirection;
        transform.rotation = Quaternion.LookRotation(newDirection);
        currentState = FakeBubbleState.Moving;
    }

    protected virtual void Reflect(Vector3 normal)
    {
        if (playerCollider != null && sphereCollider != null)
            Physics.IgnoreCollision(sphereCollider, playerCollider, false);

        direction = Vector3.Reflect(direction, normal).normalized;
        direction.y = 0;
        transform.rotation = Quaternion.LookRotation(direction);

        if (rangeCoroutine != null)
            StopCoroutine(rangeCoroutine);

        rangeCoroutine = StartCoroutine(BubbleRangeLimit());

        if (IsServer)
        {
            damage *= reflectDmgIncrease;
            isReflected = true;
            ReflectClientRpc(transform.position, direction, OwnerID.Value);
        }
    }

    [ClientRpc]
    protected void ReflectClientRpc(Vector3 serverPos, Vector3 newDir, int newOwnerId)
    {
        if (!IsServer && fakeCopy != null)
        {
            fakeCopy.OwnerID.Value = newOwnerId;
            fakeCopy.isReflected = true;

            fakeCopy.OnServerConfirmReflect(newDir, serverPos);

            // Clear trail renderers to prevent cross-screen stretching
            foreach (var trail in fakeCopy.GetComponentsInChildren<TrailRenderer>())
            {
                trail.Clear();
            }

            // Spawn reflection visual FX at the bounce location
            if (fakeCopy.fizzleEffect != null)
            {
                Instantiate(fakeCopy.fizzleEffect, serverPos, Quaternion.identity);
            }

            // Reset range limit timer
            if (fakeCopy.rangeCoroutine != null)
                fakeCopy.StopCoroutine(fakeCopy.rangeCoroutine);

            fakeCopy.rangeCoroutine = fakeCopy.StartCoroutine(fakeCopy.BubbleRangeLimit());

            // Position Correction smoothing check
            float distanceToServerBounce = Vector3.Distance(fakeCopy.transform.position, serverPos);
            float threshold = 0.15f;

            if (distanceToServerBounce > threshold)
            {
                if (fakeCopy.correctionCoroutine != null)
                    fakeCopy.StopCoroutine(fakeCopy.correctionCoroutine);

                fakeCopy.correctionCoroutine = fakeCopy.StartCoroutine(fakeCopy.SmoothCorrection(serverPos, newDir));
            }
            else
            {
                fakeCopy.transform.position = serverPos;
            }
        }
    }

    private IEnumerator SmoothCorrection(Vector3 targetPos, Vector3 targetDir)
    {
        float duration = 0.06f; // Adjustment duration window (~3-4 frames)
        float elapsed = 0f;

        Vector3 startPos = transform.position;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            transform.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        transform.position = targetPos;
        correctionCoroutine = null;
    }

    public virtual void SetSlippy()
    {
        if (!IsServer) return;

        if (isSoaped)
        {
            speed += soapSecSpeedIncrease;
        }
        else
        {
            soapSecSpeedIncrease = speed * soapSecSpeedAmp;
            speed *= soapSpeedAmp;
            isSoaped = true;
        }

        SetSlippyClientRpc(transform.position, speed, isSoaped, soapSecSpeedIncrease);
    }

    [ClientRpc]
    protected void SetSlippyClientRpc(Vector3 serverPos, float newSpeed, bool soapedState, float secSpeedIncrease)
    {
        if (!IsServer && fakeCopy != null)
        {
            fakeCopy.transform.position = serverPos;
            fakeCopy.speed = newSpeed;
            fakeCopy.isSoaped = soapedState;
            fakeCopy.soapSecSpeedIncrease = secSpeedIncrease;
        }
    }

    public void ChangeSpeed(float factor)
    {
        if (!IsServer) return;
        speed *= factor;
        ChangeSpeedClientRpc(transform.position, speed);
    }

    [ClientRpc]
    protected void ChangeSpeedClientRpc(Vector3 serverPos, float newSpeed)
    {
        if (!IsServer && fakeCopy != null)
        {
            fakeCopy.transform.position = serverPos;
            fakeCopy.speed = newSpeed;
        }
    }

    [ClientRpc]
    protected virtual void SpawnPopEffectClientRpc(Vector3 pos)
    {
        if (!IsServer && GameManager.Instance.Players[OwnerID.Value].IsOwner) return;

        if (fizzleEffect == null) return;
        Instantiate(fizzleEffect, pos, Quaternion.identity);
    }

    [ClientRpc]
    public void MakeFakePopAswellClientRpc(Vector3 serverPopPos, bool hitPlayer)
    {
        if (!IsServer && fakeCopy != null)
        {
            fakeCopy.transform.position = serverPopPos;
            if (hitPlayer)
            {
                fakeCopy.fizzleEffect = fakeCopy.hitEffect;
            }
            fakeCopy.OnServerConfirmPop();
        }
    }

    public void HideVisualsAndDisablePhysics()
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = false;
        }
        foreach (var col in GetComponentsInChildren<Collider>())
        {
            col.enabled = false;
        }
    }

    private void DestroyBubble()
    {
        if (!IsServer && !isLocalFake) return;

        if (IsServer)
            NetworkObject.Despawn(true);

        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameEnded -= DestroyBubble;
    }

    protected void IncrementMissedShotAchievement()
    {
        if (!IsServer && !isLocalFake) return;

        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)OwnerID.Value
            || !AchievementSaveSystem.instance || SceneManager.GetActiveScene().buildIndex == 5) return;

        AchievementSaveSystem achSaveSystem = AchievementSaveSystem.instance;
        achSaveSystem.IncrementStat(21, 1);
    }
}