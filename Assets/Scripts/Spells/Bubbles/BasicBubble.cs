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
    public bool IsSoaped {  get { return isSoaped; } }
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
        {
            IncrementMissedShotAchievement();
            GameManager.Instance.OnWeaponMissed(OwnerID.Value);
        }

        Pop();
    }

    private void OnTriggerEnter(Collider other)
    {    
        if (hasPopped || !isLocalFake) return;
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
        //Debug.Log("Collided with: " + collision.transform.name);
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
        if (hasPopped) return;
        if (other.CompareTag("Player"))
        {
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
                gameManager.ChangeHitReference(OwnerID.Value, spellType, player.PlayerID, isSoaped, isReflected, false, AssignedSpellID.Value);
                if (!isUlt) playerCollider.GetComponent<PlayerController>().GainUltCharge(damage, true);
            }

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
        else if (isLocalFake && other.CompareTag("Puddle"))
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
        if (!IsServer && !isLocalFake) return;

        if(IsServer)
            MakeFakePopAswellClientRpc();

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
        if (!IsServer && !isLocalFake) return;

        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)OwnerID.Value
            || !AchievementSaveSystem.instance || SceneManager.GetActiveScene().buildIndex == 5) return;
        //Debug.Log("Incrementing missed shot ach");
        AchievementSaveSystem achSaveSystem = AchievementSaveSystem.instance;
        achSaveSystem.IncrementStat(21, 1);
    }

    [ClientRpc]
    public void MakeFakePopAswellClientRpc()
    {
        if (!IsServer && fakeCopy != null)
        {
            fakeCopy.fizzleEffect = hitEffect;
            fakeCopy.Pop();
        }
    }
}