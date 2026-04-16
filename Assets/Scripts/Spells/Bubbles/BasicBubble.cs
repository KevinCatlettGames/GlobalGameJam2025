using System.Collections;
using UnityEngine;
using FMODUnity;
using Unity.Netcode;
using System.Collections.Generic;

public class BasicBubble : NetworkBehaviour
{
    public enum SpellType
    {
        Null,
        Basic,
        Exploding,
        Giant,
        SmallerGiant,
        Homing,
        Revolver,
        Snipe,
        Soap,
        Wall,
        Grenade,
        Demolish,
        Ink,
        Boomerang,
        Blast,
        Harpoon,
        Slasher
    };

    public SpellType spellType;
    public int OwnerID = -1;
    protected Vector3 direction;
    protected bool hasPopped;
    [HideInInspector] public bool HasPopped {  get { return hasPopped; } }

    [Header("Bubble Base Stats")]
    [SerializeField] protected float size = 1f;
    [SerializeField] protected float damage = 1.0f;
    [SerializeField] protected float knockback = 1.0f;
    [SerializeField] protected float speed = 1.0f;
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
    private float soapSpeedAmp = 2f;
    private float soapSecSpeedAmp = .5f;
    private float soapSecSpeedIncrease = 0f;

    protected Vector3 lastPosition;
    protected float desyncThreshold = 0.05f;
    
    protected bool canMiss = true;
    protected bool isUlt = false;
    
    private void Start()
    {
        GameManager.Instance.OnGameEnded += DestroyBubble;
    }

    public virtual void InitialiseBubble(int ID, Vector3 dir, EventReference soundEvent, Collider playerCollider)
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
            foreach(Collider col in ignoredColliders)
            {
                Physics.IgnoreCollision(sphereCollider,col);
            }
            StartCoroutine(Inflate());
        }
    }

    private void FixedUpdate()
    {
        BubbleMovement();
    }
    private IEnumerator Inflate()
    {
        sphereCollider.excludeLayers += LayerMask.GetMask("Player");
        while (currentSize < size)
        {
            currentSize += inflationSpeed * Time.deltaTime;
            if (currentSize > size) currentSize = size;

            transform.localScale = Vector3.one * currentSize;
            yield return null;
        }

        InflateOverlapChack();

        sphereCollider.excludeLayers -= LayerMask.GetMask("Player");
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
        if (!IsServer) return;

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
        
        if(canMiss) 
            IncrementMissedShotAchievement();
        
        Pop();
    }  
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer || hasPopped) return;
        Vector3 reflectNormal = collision.GetContact(0).normal;
        HandleCollision(collision);
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

        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<PlayerController>();
            GameManager gameManager = GameManager.Instance;

            if (gameManager.PlayingLocal)
                player.ApplyKnockbackLocal(OwnerID, direction, knockback, damage);
            else
                player.ApplyKnockbackServerRpc(OwnerID, direction, knockback, damage);

            gameManager.ChangeHitReference(OwnerID, spellType, player.PlayerID, isSoaped, isReflected);
            if(!isUlt) playerCollider.GetComponent<PlayerController>().GainUltCharge(damage, true);
            fizzleEffect = hitEffect;
            if (popOnPlayerHit)
                Pop();
        }
        else if (other.CompareTag("Bubble"))
        {
            if(popOnBubbleHit)
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
        
        SpawnPopEffectClientRpc(transform.position);

        if (IsServer)
        {
            NetworkObject.Despawn(true);
            Destroy(gameObject);
        }
    }  
    protected virtual void Reflect(Vector3 normal)
    {
        if (!IsServer) return;
        if (playerCollider != null)
            Physics.IgnoreCollision(sphereCollider, playerCollider, false);

        direction = Vector3.Reflect(direction, normal).normalized;
        direction.y = 0;
        transform.rotation = Quaternion.LookRotation(direction);

        if (rangeCoroutine != null)
            StopCoroutine(rangeCoroutine);
        
        rangeCoroutine = StartCoroutine(BubbleRangeLimit());
        isReflected = true;
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
            IncreaseSpeed(soapSpeedAmp);
            isSoaped = true;
        }
    }
    public void IncreaseSpeed(float inceaseFactor)
    {
        if (!IsServer) return;
        speed *= inceaseFactor;
    }
    [ClientRpc]
    private void SpawnPopEffectClientRpc(Vector3 pos)
    {
        var effect = Instantiate(fizzleEffect, pos, Quaternion.identity);
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