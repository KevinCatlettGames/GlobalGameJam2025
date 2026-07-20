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
    public NetworkVariable<int> OwnerID = new NetworkVariable<int>();
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

    public bool isLocalFake = false;

    private void Start()
    {
        GameManager.Instance.OnGameEnded += DestroyBubble;
    }

    public virtual void InitialiseBubble(int ID, Vector3 dir, Collider playerCollider)
    {
        OwnerID.Value = ID;
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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!IsOwner)
            RuntimeManager.PlayOneShotAttached(soundEvent, gameObject);

        OwnerID.OnValueChanged += OnOwnerIdAssigned;
    }

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();
        OwnerID.OnValueChanged -= OnOwnerIdAssigned;
    }

    void OnOwnerIdAssigned(int previousValue, int newValue)
    {
        CheckAndHideVisibility(newValue);
    }

    void CheckAndHideVisibility(int currentCasterId)
    {
        if (IsServer || isLocalFake) return;

        // Guard clause for array indexing safely:
        // Make sure the ID isn't negative, fits in the array, and the element isn't null
        if (currentCasterId < 0 || currentCasterId >= GameManager.Instance.Players.Length) return;
        if (GameManager.Instance.Players[currentCasterId] == null) return;

        // Check if the player in that array index is the local owner
        if (GameManager.Instance.Players[currentCasterId].IsOwner)
        {
            Debug.Log("Disabling visibility for the casting client to prevent duplicate visuals.");
            foreach (var renderer in GetComponentsInChildren<Renderer>())
                renderer.enabled = false;

            foreach (ParticleSystem particleSystem in GetComponentsInChildren<ParticleSystem>())
                particleSystem.gameObject.SetActive(false);

            foreach(TrailRenderer trailRenderer in GetComponentsInChildren<TrailRenderer>())
                trailRenderer.enabled = false;
        }
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

    protected virtual IEnumerator BubbleRangeLimit()
    {
        float lifetime = range / speed;
        yield return new WaitForSeconds(lifetime);

        if (canMiss)
            IncrementMissedShotAchievement();

        Pop();
    }

    private void OnTriggerEnter(Collider other)
    {    
        if (hasPopped || !isLocalFake) return;
        HandleTrigger(other);
    }

    private void HandleTrigger(Collider other)
    {
        if (other.gameObject.TryGetComponent<Reflector>(out var reflector) && reflector.GetIsReflecting())
        {
            OwnerID.Value = reflector.OwnerID;

            Collider myCollider = GetComponent<Collider>();
            Vector3 reflectNormal = Vector3.up;

            bool hasOverlap = Physics.ComputePenetration(
                myCollider, transform.position, transform.rotation,
                other, other.transform.position, other.transform.rotation,
                out Vector3 direction, out float distance
            );

            if (hasOverlap)
                reflectNormal = direction;
            else
                reflectNormal = (transform.position - other.transform.position).normalized;

            Reflect(reflectNormal);
            return;
        }

        if(isLocalFake && other.CompareTag("Bubble") && other.GetComponent<NetworkObject>().IsSpawned || isLocalFake && other.CompareTag("Puddle"))
            return;

        BubbleCollision(other.gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasPopped) return;
        HandleCollision(collision);
    }

    private void HandleCollision(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<Reflector>(out var reflector) && reflector.GetIsReflecting())
        {
            if (IsServer || isLocalFake) OwnerID.Value = reflector.OwnerID;
            Vector3 reflectNormal = collision.GetContact(0).normal;
            Reflect(reflectNormal);
            return;
        }

        if (IsServer || isLocalFake)
            BubbleCollision(collision.gameObject);
    }

    public virtual void BubbleCollision(GameObject other)
    {
        if (isLocalFake)
            Debug.Log("Fake in main bubble collision");

        if (hasPopped) return;
        if (other.CompareTag("Player"))
        {
            if (isLocalFake)
                Debug.Log("Fake pop with player");

            if (IsServer)
            {
                var player = other.GetComponent<PlayerController>();
                GameManager gameManager = GameManager.Instance;
                if (gameManager.PlayingLocal)
                    player.ApplyKnockbackLocal(OwnerID.Value, direction, knockback, damage);
                else
                {
                    if (IsOwner && !isLocalFake)
                        player.ApplyKnockbackServerRpc(OwnerID.Value, direction, knockback, damage);
                }
                gameManager.ChangeHitReference(OwnerID.Value, spellType, player.PlayerID, isSoaped, isReflected);
                if (!isUlt) playerCollider.GetComponent<PlayerController>().GainUltCharge(damage, true);
            }

            fizzleEffect = hitEffect;
            hasHitPlayer = true;
            if (popOnPlayerHit)
            {
                if (isLocalFake)
                    Debug.Log("Fake popping on player hit");
                Pop();
            }
        }
        else if (other.CompareTag("Bubble"))
        {
            if (isLocalFake)
            {
                Debug.Log("Fake pop with bubble");
            }

            if (popOnBubbleHit)
            {
                Debug.Log("Fake popping on bubble hit");
                Pop();
            }
        }
        else if (isLocalFake && other.CompareTag("Puddle"))
        {
            Debug.Log("Fake trigger on Puddle");
            return;
        }
        else
        {
            Debug.Log("Fake popping on something else");
            Pop();
        }
    }

    protected virtual void Pop()
    {
        if (isLocalFake)
            Debug.Log("Fake in main pop");

        if (hasPopped) return;
        if (!IsServer && !isLocalFake) return;

        hasPopped = true;
        StopAllCoroutines();

        if (IsServer)
        {
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
        if (isLocalFake)
            Debug.Log("Fake in reflect");

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
        }
        isReflected = true;
    }

    public virtual void SetSlippy()
    {
        if (isLocalFake)
            Debug.Log("Fake in set slippy");

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
        if (isLocalFake)
            Debug.Log("Fake in change speed");
        if (!IsServer && !isLocalFake) return;
        speed *= factor;
    }

    [ClientRpc]
    protected virtual void SpawnPopEffectClientRpc(Vector3 pos)
    {
        if (!IsServer && GameManager.Instance.Players[OwnerID.Value].IsOwner) return; 

        if (fizzleEffect == null) return;
        Instantiate(fizzleEffect, pos, Quaternion.identity);
    }


    private void DestroyBubble()
    {
        if (!IsServer && !isLocalFake) return;

        if(IsServer)
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
            NetworkManager.Singleton.LocalClientId != (ulong)OwnerID.Value || !SteamIntegration.instance) return;

        SteamIntegration steamIntegration = SteamIntegration.instance;
        SteamIntegration.instance.IncrementIntSteamStat(steamIntegration.missedShotStatID,
            1,
            steamIntegration.StatThresholds[steamIntegration.missedShotStatID],
            steamIntegration.missedShotAchievementID);
    }
}