using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Components;
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

    [HideInInspector] public bool isLocalFake = false;
    // --- BACK TO SIMPLE PRIMITIVES ---
    [HideInInspector]
    public NetworkVariable<int> syncedCastID = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private int _localFakeCastID;

    public int castID
    {
        get => isLocalFake ? _localFakeCastID : syncedCastID.Value;
        set
        {
            if (isLocalFake) _localFakeCastID = value;
            else if (IsServer) syncedCastID.Value = value;
        }
    }
    private bool isMeshHiddenForOwner = false;
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

    // --- FIXED: NATIVE NETCODE ENTRY POINT FOR PERFECT TIMING HANDOFFS ---
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        Debug.Log("On network spawned");
        // Fakes do not spawn on the network, so this will only ever run on real bubbles
        if (!IsServer && NetworkManager.Singleton != null)
        {
            // Fallback: If playerCollider wasn't set locally yet, find it via the OwnerID 
            if (playerCollider == null)
            {
                var players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
                foreach (var p in players)
                {
                    if (p.PlayerID == OwnerID)
                    {
                        playerCollider = p.Controller;
                        break;
                    }
                }
            }

            // Check if this incoming real bubble belongs to us!
            if (NetworkManager.Singleton.LocalClientId == (ulong)OwnerID)
            {
                SetMeshVisibility(false); // Hide the real bubble mesh instantly
                isMeshHiddenForOwner = true;

                // Start the brief countdown to swap the fake out for this real one
                StartCoroutine(HandOffCountdown());
            }
        }
    }


    private IEnumerator HandOffCountdown()
    {
        // Wait a tiny fraction of a second for the network position and variables to settle
        yield return new WaitForSeconds(0.12f);
        Debug.Log("Handing off");

        if (playerCollider != null)
        {
            var playerCtrl = playerCollider.GetComponent<PlayerController>();
            if (playerCtrl != null)
            {
                BasicBubble localFake = playerCtrl.GetLocalFakeByCastID(this.castID);
                if (localFake != null)
                {
                    Debug.Log("Found localfake");
                    localFake.HideVisualsAndDisablePhysics();
                    Destroy(localFake.gameObject);
                }
            }
        }

        // Show the real server-authoritative bubble mesh now!
        SetMeshVisibility(true);
        isMeshHiddenForOwner = false;
    }

    private void SetMeshVisibility(bool visible)
    {
        foreach (var renderer in GetComponentsInChildren<Renderer>())
        {
            renderer.enabled = visible;
        }
        Debug.Log("Change mesh visibility: " + visible);
    }

    private void FixedUpdate()
    {
        // --- FIXED: MODERN NETCODE PERMISSION ENGINE CHECKS ---
        // Checks NetworkManager status safely to prevent early-frame freeze glitches
        if (isLocalFake)
        {
            BubbleMovement();
        }
        else if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            BubbleMovement();
        }
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
        transform.position += direction * speed * Time.fixedDeltaTime;

        if (Vector3.Distance(transform.position, lastPosition) > desyncThreshold)
        {
            lastPosition = transform.position;
        }
    }

    protected virtual IEnumerator BubbleRangeLimit()
    {
        float lifetime = range / speed;
        yield return new WaitForSeconds(lifetime);

        if (canMiss)
            IncrementMissedShotAchievement();

        Pop();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasPopped) return;
        if (!IsServer && !isLocalFake) return;

        HandleCollision(collision);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasPopped || other == null) return;
        if (!IsServer && !isLocalFake) return;

        if (other.gameObject.TryGetComponent<Reflector>(out var reflector) && reflector.GetIsReflecting())
        {
            OwnerID = reflector.OwnerID;
            // Approximate a normal vector since triggers don't have contact points
            Vector3 approximateNormal = (transform.position - other.transform.position).normalized;
            Reflect(approximateNormal);
        }
    }

    private void HandleCollision(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<Reflector>(out var reflector) && reflector.GetIsReflecting())
        {
            OwnerID = reflector.OwnerID;
            Vector3 reflectNormal = collision.GetContact(0).normal;
            Reflect(reflectNormal);
            return;
        }

        BubbleCollision(collision.gameObject);
    }

    public virtual void BubbleCollision(GameObject other)
    {
        if (hasPopped) return;

        if (isLocalFake)
        {
            if (other != null && other.CompareTag("Player"))
            {
                if (!ignoredColliders.Contains(other.GetComponent<Collider>()))
                {
                    Pop();
                }
            }
            else
            {
                Pop();
            }
            return;
        }

        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<PlayerController>();
            GameManager gameManager = GameManager.Instance;

            if (gameManager.PlayingLocal)
                player.ApplyKnockbackLocal(OwnerID, direction, knockback, damage);
            else
            {
                if (IsOwner)
                    player.ApplyKnockbackServerRpc(OwnerID, direction, knockback, damage);
            }

            gameManager.ChangeHitReference(OwnerID, spellType, player.PlayerID, isSoaped, isReflected);
            if (!isUlt) playerCollider.GetComponent<PlayerController>().GainUltCharge(damage, true);
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
        hasPopped = true;

        StopAllCoroutines();

        // --- FIXED: LOCAL INSTANTIATION GUARD FOR FAKES vs SERVER REAL BUBBLES ---
        if (isLocalFake)
        {
            // If it's our local fake, spawn the effect immediately on our screen
            if (fizzleEffect != null) Instantiate(fizzleEffect, transform.position, Quaternion.identity);

            HideVisualsAndDisablePhysics();
            Destroy(gameObject, 0.05f);
        }
        else if (IsServer)
        {
            // The server tells EVERYONE (including the host and other clients) to spawn the effect
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

        // Calculate bounce direction
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
        else if (isLocalFake)
        {
            ignoredColliders.Clear();
        }
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
            ChangeSpeed(soapSpeedAmp);
            isSoaped = true;
        }
    }

    public void ChangeSpeed(float factor)
    {
        if (!IsServer) return;
        speed *= factor;
    }

    [ClientRpc]
    protected virtual void SpawnPopEffectClientRpc(Vector3 pos)
    {
        // --- FIXED: PREVENT DOUBLE EFFECTS FOR THE CASTER ---
        // If this client already predicted and spawned the pop particle via their local fake, stop here!
        if (!IsServer && NetworkManager.Singleton.LocalClientId == (ulong)OwnerID)
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
            NetworkManager.Singleton.LocalClientId != (ulong)OwnerID
            || !SteamIntegration.instance) return;

        SteamIntegration steamIntegration = SteamIntegration.instance;
        SteamIntegration.instance.IncrementIntSteamStat(steamIntegration.missedShotStatID,
            1,
            steamIntegration.StatThresholds[steamIntegration.missedShotStatID],
            steamIntegration.missedShotAchievementID);
    }
}