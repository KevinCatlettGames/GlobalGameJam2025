using System.Collections;
using UnityEngine;
using FMODUnity;

public class BasicBubble : MonoBehaviour
{
    protected float damage = 1.0f;
    protected float knockback = 1.0f;
    protected float speed = 1.0f;
    protected float range = 1.0f;
    protected float size = 1.0f;
    protected float currentSize = 0.01f;
    protected float inflationSpeed = 8f;
    protected Vector3 direction = Vector3.zero;
    protected Coroutine rangeCoroutine;
    protected bool hasPopped = false;
    protected SphereCollider sphereCollider;
    [HideInInspector] public bool isSlippy = false;
    protected float slippMod = 2f;
    protected Collider playerCollider;
    [HideInInspector] public int OwnerID = -1;
    [SerializeField] protected GameObject popEffect;

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
        sphereCollider = GetComponent<SphereCollider>();
        this.playerCollider = playerCollider;
        if (sphereCollider != null) 
        {
            if(playerCollider != null) Physics.IgnoreCollision(sphereCollider, playerCollider, true);
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
        transform.position += direction * speed * Time.fixedDeltaTime;
    }
    protected IEnumerator BubbleRangeLimit()
    {
        float killTime = 0;
        killTime = range / speed;
        yield return new WaitForSeconds(killTime);
        Pop();
    }

    protected virtual void Pop()
    {
        if(hasPopped) return;
        hasPopped = true;
        StopCoroutine(rangeCoroutine);
        GameObject effect = Instantiate(popEffect, transform.position, Quaternion.identity);
        BubbleEffect bubbleEffect = effect.GetComponent<BubbleEffect>();
        bubbleEffect?.Initialise(size);

        Destroy(gameObject);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (hasPopped) return;
        Reflector reflector;
        if (collision.gameObject.TryGetComponent<Reflector>(out reflector))
        {
            if (reflector.GetIsReflecting())
            {
                OwnerID = reflector.OwnerID;
                Reflect(collision.GetContact(0).normal);
                return;
            }
        }
        BubbleCollision(collision.gameObject);
    }

    public virtual void BubbleCollision(GameObject other)
    {
        if (hasPopped) return;
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            player.ApplyKnockback(OwnerID, direction, knockback, damage);
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
            yield return new WaitForEndOfFrame();
        }
        sphereCollider.enabled = true;
    }
    private void Reflect(Vector3 normal)
    {
        if (playerCollider != null) Physics.IgnoreCollision(sphereCollider, playerCollider, false);
        direction = Vector3.Reflect(direction, normal);
        direction = new Vector3(direction.x, 0, direction.z);
        StopCoroutine(rangeCoroutine);
        rangeCoroutine = StartCoroutine(BubbleRangeLimit());
    }
    public virtual void SetSlippy()
    {
        if(!isSlippy)
        {
            isSlippy = true;
            speed *= slippMod;
        }
        
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
