using System.Collections;
using UnityEngine;
using FMODUnity;
using Unity.Netcode;

public class BasicBubble : NetworkBehaviour
{
    public int OwnerID;
    public Vector3 direction;
    public bool hasPopped;
    public float size;
    private bool slippyApplied = false;
    private bool canReflect = true;
    
    public float damage = 1.0f;
    public float knockback = 1.0f;
    public float speed = 1.0f;
    public float range = 1.0f;
    public Coroutine rangeCoroutine;
    public SphereCollider sphereCollider;
    public float currentSize = 0.01f;
    public Collider playerCollider;
    public bool isSlippy = false;

    [SerializeField] private GameObject popEffect;
    [SerializeField] protected float inflationSpeed = 8f;
    [SerializeField] private float slippMod = 2f;

    private Vector3 lastPosition;
    private float desyncThreshold = 0.05f;
    
    private void Start()
    {
        GameManager.Instance.OnGameStarted += DestroyBubble;
    }

    public virtual void InitialiseBubble(int ID, float dmg, float knb, float spd, float rng, float siz, float inf, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        OwnerID = ID;
        damage = dmg;
        knockback = knb;
        speed = spd;
        range = rng;
        size = siz;
        direction = dir;
        inflationSpeed = inf;
        this.playerCollider = playerCollider;

        rangeCoroutine = StartCoroutine(BubbleRangeLimit());

        RuntimeManager.PlayOneShotAttached(soundEvent, gameObject);

        sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider != null)
        {
            sphereCollider.enabled = false;
            StartCoroutine(Inflate());
        }
    }

    private void FixedUpdate()
    {
        BubbleMovement();
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
    
    protected IEnumerator BubbleRangeLimit()
    {
        float lifetime = range / speed;
        yield return new WaitForSeconds(lifetime);
        Pop();
    }
    
    protected virtual void Pop()
    {
        if (hasPopped) return;

        hasPopped = true;

        if (rangeCoroutine != null)
            StopCoroutine(rangeCoroutine);
        
        SpawnPopEffectClientRpc(transform.position, size);

        if (IsServer)
        {
            NetworkObject.Despawn(true);
            Destroy(gameObject);
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer || hasPopped) return;

        HandleCollision(collision.gameObject);
    }
    
    private void HandleCollision(GameObject other)
    {
        if (other.TryGetComponent<Reflector>(out var reflector) && reflector.GetIsReflecting())
        {
            OwnerID = reflector.OwnerID;
            Vector3 reflectNormal = other.GetComponent<Collider>().ClosestPointOnBounds(transform.position);
            Reflect(reflectNormal);
            return;
        }

        BubbleCollision(other);
    }
    
    public virtual void BubbleCollision(GameObject other)
    {
        if (hasPopped) return;

        if (other.CompareTag("Player") && other.GetComponent<Collider>() != playerCollider)
        {
            var player = other.GetComponent<PlayerController>();

            if (GameManager.Instance.playingLocal)
                player.ApplyKnockbackLocal(OwnerID, direction, knockback, damage);
            else
                player.ApplyKnockbackServerRpc(OwnerID, direction, knockback, damage);
        }

        Pop();
    }
    
    protected IEnumerator Inflate()
    {
        while (currentSize < size)
        {
            currentSize += inflationSpeed * Time.deltaTime;
            if (currentSize > size) currentSize = size;

            transform.localScale = Vector3.one * currentSize;
            yield return null;
        }

        Collider[] overlaps = Physics.OverlapSphere(transform.position, size, LayerMask.GetMask("Player"));

        foreach (var col in overlaps)
        {
            if (col == playerCollider) continue;
            BubbleCollision(col.gameObject);
            break;
        }

        sphereCollider.enabled = true;
    }
    
    private void Reflect(Vector3 normal)
    {
        if (!IsServer || !canReflect) return;

        canReflect = false;
        StartCoroutine(ReflectCooldown());

        if (playerCollider != null)
            Physics.IgnoreCollision(sphereCollider, playerCollider, false);

        direction = Vector3.Reflect(direction, normal).normalized;
        direction.y = 0;
        transform.rotation = Quaternion.LookRotation(direction);

        if (rangeCoroutine != null)
            StopCoroutine(rangeCoroutine);

        rangeCoroutine = StartCoroutine(BubbleRangeLimit());
    }

    private IEnumerator ReflectCooldown()
    {
        yield return new WaitForSeconds(0.15f);  // 150 ms cooldown
        canReflect = true;
    }
    
    public virtual void SetSlippy()
    {
        if (!IsServer) return;

        if (!slippyApplied && slippMod > 1f)
        {
            speed *= slippMod;
            slippyApplied = true;
        }
    }
    
    private void SpawnPopEffect(Vector3 pos, float scale)
    {
        if (GameManager.Instance.playingLocal)
        {
            var effect = Instantiate(popEffect, pos, Quaternion.identity);
            effect.GetComponent<BubbleEffect>()?.Initialise(scale);
        }
        else
        {
            SpawnPopEffectServerRpc(pos, scale);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SpawnPopEffectServerRpc(Vector3 pos, float scale)
    {
        SpawnPopEffectClientRpc(pos, scale);
    }

    [ClientRpc]
    private void SpawnPopEffectClientRpc(Vector3 pos, float scale)
    {
        var effect = Instantiate(popEffect, pos, Quaternion.identity);
        effect.GetComponent<BubbleEffect>()?.Initialise(scale);
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
            GameManager.Instance.OnGameStarted -= DestroyBubble;
    }
}
