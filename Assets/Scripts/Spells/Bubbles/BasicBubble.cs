using System.Collections;
using UnityEngine;
using FMODUnity;
using Unity.Netcode;

public class BasicBubble : NetworkBehaviour
{
    // Networked variables (only essential ones)
    public NetworkVariable<int> OwnerID = new NetworkVariable<int>(-1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<Vector3> direction = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> hasPopped = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> size = new NetworkVariable<float>(1.0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

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
        if (!IsServer) return; // Only the server can initialize the bubble

        OwnerID.Value = ID;
        damage = dmg;
        knockback = knb;
        speed = spd;
        range = rng;
        size.Value = siz;
        direction.Value = dir;
        inflationSpeed = inf;
        rangeCoroutine = StartCoroutine(BubbleRangeLimit());
        RuntimeManager.PlayOneShotAttached(soundEvent, gameObject);
        this.playerCollider = playerCollider;

        sphereCollider = GetComponent<SphereCollider>();
        if (sphereCollider != null)
        {
            if (playerCollider != null) Physics.IgnoreCollision(sphereCollider, playerCollider, true);
            sphereCollider.enabled = false;
            StartCoroutine(Inflate());
        }
    }

    private void FixedUpdate()
    {
        if (IsServer)
        {
            // Server updates position and movement
            BubbleMovement();
        }
    }

    protected virtual void BubbleMovement()
    {
        transform.position += direction.Value * speed * Time.fixedDeltaTime;
    }

    protected IEnumerator BubbleRangeLimit()
    {
        float killTime = range / speed;
        yield return new WaitForSeconds(killTime);
        Pop();
    }

    protected virtual void Pop()
    {
        if (hasPopped.Value) return; // Check if already popped

        // Call ServerRpc to set the hasPopped value on the server
        SetHasPoppedServerRpc(true);

        StopCoroutine(rangeCoroutine);

        // Spawn pop effect for clients
        SpawnPopEffectClientRpc(transform.position, size.Value);

        // Destroy bubble after popping
        GetComponent<NetworkObject>().Despawn(gameObject);
    }

    // ServerRpc to set hasPopped
    [ServerRpc(RequireOwnership = false)]
    private void SetHasPoppedServerRpc(bool popped)
    {
        hasPopped.Value = popped;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasPopped.Value) return;

        // Only the server handles critical collision logic like popping the bubble
        if (IsServer)
        {
            HandleCollision(collision.gameObject);
        }
        else
        {
            var collisionNetworkObjectReference = collision.gameObject.GetComponent<NetworkObject>();
            // Client simply informs the server that the collision occurred
            HandleCollisionClientRpc(collisionNetworkObjectReference);
        }
    }

    // This method is called on the server to handle the actual logic of popping and applying effects
    private void HandleCollision(GameObject other)
    {
        Reflector reflector;
        if (other.TryGetComponent<Reflector>(out reflector))
        {
            if (reflector.GetIsReflecting())
            {
                OwnerID.Value = reflector.OwnerID;
                Reflect(other.GetComponent<Collider>().ClosestPointOnBounds(transform.position));
                return;
            }
        }
        
        BubbleCollision(other);
    }

    // ClientRpc to inform the server of a collision (called by client)
    [ClientRpc]
    private void HandleCollisionClientRpc(NetworkObjectReference collisionNetworkObjectReference)
    {
        if (collisionNetworkObjectReference.TryGet(out NetworkObject collisionNetObj))
        {

            if (hasPopped.Value) return;
            if (collisionNetObj.CompareTag("Player"))
            {
                PlayerController player = collisionNetObj.GetComponent<PlayerController>();
                player.ApplyKnockbackServerRpc(OwnerID.Value, direction.Value, knockback, damage);
            }

            Pop();
        }
    }

    public virtual void BubbleCollision(GameObject other)
    {
        if (hasPopped.Value) return;

        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            player.ApplyKnockbackServerRpc(OwnerID.Value, direction.Value, knockback, damage);
        }
        Pop();
    }

    protected IEnumerator Inflate()
    {
        while (currentSize < size.Value)
        {
            currentSize += inflationSpeed * Time.deltaTime;
            transform.localScale = Vector3.one * currentSize;
            if (currentSize > size.Value) currentSize = size.Value;
            yield return new WaitForEndOfFrame();
        }
        sphereCollider.enabled = true;
    }

    private void Reflect(Vector3 normal)
    {
        if (playerCollider != null) Physics.IgnoreCollision(sphereCollider, playerCollider, false);
        direction.Value = Vector3.Reflect(direction.Value, normal);
        direction.Value = new Vector3(direction.Value.x, 0, direction.Value.z);
        transform.rotation = Quaternion.LookRotation(direction.Value);
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

    // ClientRpc to spawn pop effect on clients
    [ClientRpc]
    private void SpawnPopEffectClientRpc(Vector3 pos, float scale)
    {
        GameObject effect = Instantiate(popEffect, pos, Quaternion.identity);
        BubbleEffect bubbleEffect = effect.GetComponent<BubbleEffect>();
        bubbleEffect?.Initialise(scale);
    }
    
    private void DestroyBubble()
    {
        Destroy(gameObject);
    }
    private void OnDestroy()
    {
        GameManager.Instance.OnGameStarted -= DestroyBubble;
    }
}
