using System.Collections;
using UnityEngine;
using FMODUnity;
using Unity.Netcode;

public class BasicBubble : NetworkBehaviour
{
    // Networked variables (only essential ones)
    public int OwnerID;
    public Vector3 direction;
    public bool hasPopped;
    public float size;

    // Local variables (no need for network sync)
    public float damage = 1.0f;
    public float knockback = 1.0f;
    public float speed = 1.0f;
    public float range = 1.0f;
    public Coroutine rangeCoroutine;
    public SphereCollider sphereCollider;
    public float currentSize = 0.01f;
    public Collider playerCollider;
    public bool isSlippy = false;
    [SerializeField] public GameObject popEffect;
    [SerializeField] public float inflationSpeed = 8f;
    [SerializeField] public float slippMod = 2f;
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
        rangeCoroutine = StartCoroutine(BubbleRangeLimit());
        RuntimeManager.PlayOneShotAttached(soundEvent, gameObject);
        this.playerCollider = playerCollider;

        if (playerCollider != null)
        {
            Physics.IgnoreCollision(GetComponent<Collider>(), playerCollider, true);
            StartCoroutine(ReenableCollisionAfterDelay(1f)); // Delay in seconds
        }
        
        sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider != null)
        {
            sphereCollider.enabled = false;
            StartCoroutine(Inflate());
        }
    }
    
    private IEnumerator ReenableCollisionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (playerCollider != null)
        {
            Physics.IgnoreCollision(GetComponent<Collider>(), playerCollider, false);
        }
    }


    private void FixedUpdate()
    { 
        BubbleMovement();
    }

    protected virtual void BubbleMovement()
    {
        transform.position += direction * speed * Time.fixedDeltaTime;
    }

    protected IEnumerator BubbleRangeLimit()
    {
        float killTime = range / speed;
        yield return new WaitForSeconds(killTime);
        Pop();
    }

    protected virtual void Pop()
    {
        if (hasPopped) return; // Check if already popped

        // Call ServerRpc to set the hasPopped value on the server
        SetHasPopped(true);

        if(rangeCoroutine != null) 
            StopCoroutine(rangeCoroutine);

        // Spawn pop effect for clients
        SpawnPopEffect(transform.position, size);

        if (IsServer)
        {
            GetComponent<Unity.Netcode.NetworkObject>().Despawn();
            Destroy(gameObject);
        }
    }
    
    private void SetHasPopped(bool popped)
    {
        hasPopped = popped;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasPopped) return; 
        
        HandleCollision(collision.gameObject);
        
    }

    // This method is called on the server to handle the actual logic of popping and applying effects
    private void HandleCollision(GameObject other)
    {
        Reflector reflector;
        if (other.TryGetComponent<Reflector>(out reflector))
        {
            if (reflector.GetIsReflecting())
            {
                OwnerID = reflector.OwnerID;
                Reflect(other.GetComponent<Collider>().ClosestPointOnBounds(transform.position));
                return;
            }
        }
        
        BubbleCollision(other);
    }
    
    public virtual void BubbleCollision(GameObject other)
    {
        if (hasPopped) return;

        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();

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
            transform.localScale = Vector3.one * currentSize;
            if (currentSize > size) currentSize = size;
            yield return new WaitForEndOfFrame();
        }
        Collider[] overlaps = Physics.OverlapSphere(transform.position, size, LayerMask.GetMask("Player"));
        if (overlaps.Length > 0)
        {
            for (int i = 0; i < overlaps.Length; i++)
            {
                if (overlaps[i] == playerCollider)
                {
                    continue;
                }
                else
                {
                    BubbleCollision(overlaps[i].gameObject);
                    break;
                }

            }
        }
        sphereCollider.enabled = true;

    }

    private void Reflect(Vector3 normal)
    {
        if (playerCollider != null) Physics.IgnoreCollision(sphereCollider, playerCollider, false);
        direction = Vector3.Reflect(direction, normal);
        direction = new Vector3(direction.x, 0, direction.z);
        transform.rotation = Quaternion.LookRotation(direction);
        StopCoroutine(rangeCoroutine);
        rangeCoroutine = StartCoroutine(BubbleRangeLimit());
    }

    public virtual void SetSlippy()
    {
        if (slippMod > 1f)
        {
            speed *= slippMod;
        }
    }
    
    private void SpawnPopEffect(Vector3 pos, float scale)
    {
        if (GameManager.Instance.playingLocal)
        {
            GameObject effect = Instantiate(popEffect, pos, Quaternion.identity);
            BubbleEffect bubbleEffect = effect.GetComponent<BubbleEffect>();
            bubbleEffect?.Initialise(scale);
        }
        else
        {
            SpawnPopEffectServerRpc(pos, scale);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void SpawnPopEffectServerRpc(Vector3 pos, float scale)
    {
        SpawnPopEffectClientRpc(pos, scale);
    }

    [ClientRpc]
    void SpawnPopEffectClientRpc(Vector3 pos, float scale)
    {
        GameObject effect = Instantiate(popEffect, pos, Quaternion.identity);
        BubbleEffect bubbleEffect = effect.GetComponent<BubbleEffect>();
        bubbleEffect?.Initialise(scale);
    }
    
    private void DestroyBubble()
    {
        if (IsServer)
        {
            GetComponent<Unity.Netcode.NetworkObject>().Despawn();
            Destroy(gameObject);
        }
    }
    private void OnDestroy()
    {
        GameManager.Instance.OnGameStarted -= DestroyBubble;
    }
}
