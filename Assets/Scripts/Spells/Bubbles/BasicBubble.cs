using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BasicBubble : NetworkBehaviour
{
    public enum SpellType
    {
        Null, Basic, Exploding, Giant, SmallerGiant, Homing, Revolver, Snipe, Soap,
        Wall, Grenade, Demolish, Ink, Boomerang, Blast, Harpoon, Slasher, Zap,
        Teleport, Cross, Split
    };

    public SpellType spellType;
    public int OwnerID = -1;
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
    protected bool isReflected = false;
    protected bool hasInflated = false;

    [Header("Hit behaviour")]
    [SerializeField] protected bool popOnPlayerHit = true;
    [SerializeField] protected bool popOnBubbleHit = true;

    [Header("Effecs")]
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

    // Local Fake Variables
    [HideInInspector] public bool isLocalFake = false;
    [HideInInspector] public NetworkVariable<int> syncedCastID = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private int _localFakeCastID;
    [HideInInspector] public Transform serverBubbleTarget;
    [SerializeField] protected Transform visualChildMesh;
    [SerializeField] private float visualBlendSpeed = 3f;
    private Vector3 visualOffset;
    public int castID
    {
        get => isLocalFake ? _localFakeCastID : syncedCastID.Value;
        set
        {
            if (isLocalFake) _localFakeCastID = value;
            else if (IsServer) syncedCastID.Value = value;
        }
    }
    public bool isMeshHiddenForOwner = false;
    private bool isInitialized = false;
    private float trackingSpeed = 0f;
    public float catchUpTime = 0.2f;
    private float safetyTimer = 0f;
    public float maxTrackingDuration = 0.35f;

    private void Start()
    {
        GameManager.Instance.OnGameEnded += DestroyBubble;
    }

    public virtual void InitialiseBubble(int ID, Vector3 dir, Collider playerCollider)
    {
        OwnerID = ID;
        direction = dir;
        this.playerCollider = playerCollider;

        rangeCoroutine = StartCoroutine(BubbleRangeLimit());

        RuntimeManager.PlayOneShotAttached(soundEvent, gameObject);

        sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider != null)
        {
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

    public void InitializeReconciliation(Transform serverTarget)
    {
        serverBubbleTarget = serverTarget;
        if (visualChildMesh != null && serverTarget != null)
        {
            if (serverTarget.TryGetComponent<BasicBubble>(out var serverBubble))
            {
                this.castID = serverBubble.castID;
            }
            float currentRTT = GetCurrentRTTInSeconds();
            catchUpTime = Mathf.Clamp(currentRTT * 1.2f, 0.12f, 0.45f);
            maxTrackingDuration = Mathf.Clamp((currentRTT * 2.5f) + 0.2f, 0.4f, 1.5f);
            Vector3 worldOffset = serverTarget.position - transform.position;
            visualOffset = transform.InverseTransformDirection(worldOffset);
            if (rangeCoroutine != null)
                StopCoroutine(rangeCoroutine);
            float trueTimeSpent = worldOffset.magnitude / speed;
            float remainingServerLifetime = (range / speed) - trueTimeSpent;
            remainingServerLifetime = Mathf.Max(remainingServerLifetime, 0.05f);
            rangeCoroutine = StartCoroutine(BubbleRangeLimit(remainingServerLifetime));
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
            RuntimeManager.PlayOneShotAttached(soundEvent, gameObject);

        if (!IsServer && NetworkManager.Singleton != null)
        {
            if (syncedCastID.Value != 0)
                OnCastIDSynced();
            else
                syncedCastID.OnValueChanged += HandleCastIDChange;
        }
    }

    private void HandleCastIDChange(int previousValue, int newValue)
    {
        syncedCastID.OnValueChanged -= HandleCastIDChange;
        OnCastIDSynced();
    }

    private void OnCastIDSynced()
    {
        var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p.IsLocalPlayer)
            {
                p.TriggerHandoff(this, this.castID);
                break;
            }
        }
    }

    public void SetMeshVisibility(bool visible)
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
            renderer.enabled = visible;
    }

    private void FixedUpdate()
    {
        if (isLocalFake)
        {
            BubbleMovement();
        }
        else if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            BubbleMovement();
        }
    }

    protected virtual void Update()
    {
        if (isLocalFake)
        {
            safetyTimer += Time.deltaTime;

            if (serverBubbleTarget != null && visualChildMesh != null)
            {
                Vector3 worldOffset = serverBubbleTarget.position - transform.position;
                Vector3 currentLocalTarget = transform.InverseTransformDirection(worldOffset);

                if (!isInitialized)
                {
                    float initialDistance = Vector3.Distance(visualChildMesh.localPosition, currentLocalTarget);
                    trackingSpeed = initialDistance / catchUpTime;
                    isInitialized = true;
                }

                visualChildMesh.localPosition = Vector3.MoveTowards(
                    visualChildMesh.localPosition,
                    currentLocalTarget,
                    trackingSpeed * Time.deltaTime
                );

                if (Vector3.Distance(visualChildMesh.localPosition, currentLocalTarget) < 0.1f)
                {
                    ExecuteHandoffCleanUp();
                    return;
                }
            }

            if (safetyTimer >= maxTrackingDuration)
            {
                ExecuteHandoffCleanUp();
                return;
            }
        }
        else if (visualChildMesh != null)
        {
            visualChildMesh.localPosition = Vector3.zero;
            isInitialized = false;
        }
    }

    private void ExecuteHandoffCleanUp()
    {
        if (serverBubbleTarget != null)
        {
            BasicBubble realBubble = serverBubbleTarget.GetComponent<BasicBubble>();
            if (realBubble != null)
            {
                realBubble.SetMeshVisibility(true);
                realBubble.isMeshHiddenForOwner = false;

                var realCollider = realBubble.GetComponent<Collider>();
                if (realCollider != null) realCollider.enabled = true;
            }
        }

        serverBubbleTarget = null;
        Destroy(gameObject);
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
        InflateOverlapChack();
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
    }

    protected virtual IEnumerator BubbleRangeLimit(float customLifetime = 0f)
    {
        float lifetime = (customLifetime > 0f) ? customLifetime : (range / speed);

        yield return new WaitForSeconds(lifetime);

        if (canMiss && isLocalFake)
            IncrementMissedShotAchievement();

        Pop();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasPopped) return;
        if (!IsServer) return;
        HandleCollision(collision);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasPopped || other == null) return;

        if (!IsServer && !isLocalFake) return;

        if (other.gameObject.TryGetComponent<Reflector>(out var reflector) && reflector.GetIsReflecting())
        {
            if (IsServer)
                OwnerID = reflector.OwnerID;

            Vector3 approximateNormal = (transform.position - other.transform.position).normalized;
            Reflect(approximateNormal);
        }
    }

    private void HandleCollision(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<Reflector>(out var reflector) && reflector.GetIsReflecting())
        {
            if (IsServer) OwnerID = reflector.OwnerID;
            Vector3 reflectNormal = collision.GetContact(0).normal;
            Reflect(reflectNormal);
            return;
        }

        if (isLocalFake && !collision.gameObject.CompareTag("Player") && !collision.gameObject.CompareTag("Bubble"))
        {
            HideVisualsAndDisablePhysics();
            return;
        }

        if (IsServer)
            BubbleCollision(collision.gameObject);
    }

    public virtual void BubbleCollision(GameObject other)
    {
        if (hasPopped) return;
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<PlayerController>();
            GameManager gameManager = GameManager.Instance;
            if (gameManager.PlayingLocal)
                player.ApplyKnockbackLocal(OwnerID, direction, knockback, damage);
            else
            {
                if (IsOwner && !isLocalFake)
                    player.ApplyKnockbackServerRpc(OwnerID, direction, knockback, damage);
            }

            if (!isLocalFake)
            {
                gameManager.ChangeHitReference(OwnerID, spellType, player.PlayerID, isSoaped, isReflected);
                if (!isUlt) playerCollider.GetComponent<PlayerController>().GainUltCharge(damage, true);
            }

            fizzleEffect = hitEffect;
            hasHitPlayer = true;
            if (popOnPlayerHit)
                Pop();
        }
        else if (other.CompareTag("Bubble"))
        {
            if (popOnBubbleHit)
                Pop();
        }
        else
        {
            Pop();
        }
    }

    protected virtual void Pop()
    {
        if (hasPopped) return;
        if (!IsServer && !isLocalFake) return;

        hasPopped = true;
        StopAllCoroutines();

        if (IsServer)
        {
            SpawnPopEffectClientRpc(transform.position);

            if (playerCollider != null && !GameManager.Instance.PlayingLocal)
            {
                var shooterController = playerCollider.GetComponent<PlayerController>();
                if (shooterController != null)
                {
                    shooterController.DestroyLocalFakeBubbleClientRpc(castID);
                }
            }
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

    protected virtual void Reflect(Vector3 normal)
    {
        if (!IsServer && !isLocalFake) return;

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
        }
    }

    public virtual void SetSlippy()
    {
        if (!IsServer && !isLocalFake) return;
        if (isSoaped)
            speed += soapSecSpeedIncrease;
        else
        {
            soapSecSpeedIncrease = speed * soapSecSpeedAmp;
            ChangeSpeed(soapSpeedAmp);
            isSoaped = true;
        }
    }

    public void ChangeSpeed(float factor)
    {
        if (!IsServer && !isLocalFake) return;
        speed *= factor;
    }

    [ClientRpc]
    protected virtual void SpawnPopEffectClientRpc(Vector3 pos)
    {
        if (isMeshHiddenForOwner)
        {
            return;
        }

        if (fizzleEffect == null) return;
        Instantiate(fizzleEffect, pos, Quaternion.identity);
    }

    private void DestroyBubble()
    {
        if (!IsServer) return;
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
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay &&
            NetworkManager.Singleton.LocalClientId != (ulong)OwnerID || !SteamIntegration.instance) return;

        SteamIntegration steamIntegration = SteamIntegration.instance;
        SteamIntegration.instance.IncrementIntSteamStat(steamIntegration.missedShotStatID,
            1,
            steamIntegration.StatThresholds[steamIntegration.missedShotStatID],
            steamIntegration.missedShotAchievementID);
    }

    private void OnDrawGizmos()
    {
        if (serverBubbleTarget == null) return;
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(serverBubbleTarget.position, .5f);
    }
    private float GetCurrentRTTInSeconds()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.NetworkConfig.NetworkTransport != null)
        {
            var transport = NetworkManager.Singleton.NetworkConfig.NetworkTransport;
            ulong serverId = NetworkManager.ServerClientId;

            float rttMs = transport.GetCurrentRtt(serverId);
            return Mathf.Max(rttMs / 1000f, 0.01f);
        }
        return 0.1f;
    }
}