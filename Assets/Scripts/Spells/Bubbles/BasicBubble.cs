using System.Collections;
using UnityEngine;
using FMODUnity;
using Unity.Netcode;

public class BasicBubble : NetworkBehaviour
{
    public int OwnerID = -1;
    protected Vector3 direction;
    protected bool hasPopped;
    protected float size;
    
    protected float damage = 1.0f;
    protected float knockback = 1.0f;
    protected float speed = 1.0f;
    protected float range = 1.0f;
    protected Coroutine rangeCoroutine;
    protected SphereCollider sphereCollider;
    protected float currentSize = 0.01f;
    protected Collider playerCollider;
    protected bool isSoaped = false;
    protected float inflationSpeed = 8f;
    protected bool hasInflated = false;

    [SerializeField] protected GameObject popEffect;
    private float soapSpeedAmp = 2f;
    private float soapSecSpeedAmp = .5f;
    private float soapSecSpeedIncrease = 0f;

    protected Vector3 lastPosition;
    protected float desyncThreshold = 0.05f;
    
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
            if (playerCollider != null)
                Physics.IgnoreCollision(sphereCollider, playerCollider, true);
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
        Collider[] overlaps = Physics.OverlapSphere(transform.position, size, LayerMask.GetMask("Player", "Wall"));

        foreach (var col in overlaps)
        {
            if (col == playerCollider) continue;
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
    protected IEnumerator BubbleRangeLimit()
    {
        float lifetime = range / speed;
        yield return new WaitForSeconds(lifetime);
        Pop();
    }  
    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer || hasPopped) return;

        HandleCollision(collision);
    }  
    private void HandleCollision(Collision collision)
    {
        if (collision.gameObject.TryGetComponent<Reflector>(out var reflector) && reflector.GetIsReflecting())
        {
            OwnerID = reflector.OwnerID;
            Vector3 reflectNormal = collision.contacts[0].normal;
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

            if (GameManager.Instance.PlayingLocal)
                player.ApplyKnockbackLocal(OwnerID, direction, knockback, damage);
            else
                player.ApplyKnockbackServerRpc(OwnerID, direction, knockback, damage);
        }

        Pop();
    }
    protected virtual void Pop()
    {
        if (hasPopped) return;

        hasPopped = true;

        StopAllCoroutines();
        
        SpawnPopEffectClientRpc(transform.position, size);

        if (IsServer)
        {
            NetworkObject.Despawn(true);
            Destroy(gameObject);
        }
    }  
    private void Reflect(Vector3 normal)
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
