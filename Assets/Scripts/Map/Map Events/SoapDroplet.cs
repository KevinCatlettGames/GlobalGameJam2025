using Unity.Netcode;
using UnityEngine;

public class SoapDroplet : NetworkBehaviour
{
    [SerializeField] private GameObject soapSplash;
    [SerializeField] private float startDelay = .5f;
    [Header("Droplet Physics")]
    [SerializeField] private Transform dropletTransform;
    [SerializeField] private float startHeight = 50;
    [SerializeField] private float dropletFallSpeed = 0f;
    [SerializeField] private float gravity = 15;

    [Header("Impact Damage")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float knockback = 5f;
    [SerializeField] private float radius = 5f;
    [SerializeField] private float minSize = 1f;
    [SerializeField] private float maxSize = 2f;
    private float size = 1f; 

    private bool hasExploded = false;
    bool activeDroplet = false;

    void Start()
    {
        dropletTransform.position = new Vector3 (dropletTransform.position.x, startHeight, dropletTransform.position.z);
        dropletTransform.gameObject.SetActive(false);
        size = Random.Range(minSize, maxSize);
        transform.localScale = Vector3.one * size;
        Invoke(nameof(ActivateDroplet), startDelay);
        GameManager.Instance.OnGameStarted += DestroyOnRestart;
    }

    private void ActivateDroplet()
    {
        dropletTransform.gameObject.SetActive(true);
        activeDroplet = true;
    }
    void FixedUpdate()
    {
        if (activeDroplet && dropletTransform.position.y > 0)
        {
            dropletFallSpeed += Time.fixedDeltaTime * gravity;
            dropletTransform.position = dropletTransform.position + Vector3.down * dropletFallSpeed * Time.fixedDeltaTime;
        }
        else if (activeDroplet)
        {
            if (hasExploded) return;

            if (IsServer)
            {
                GameObject splash = Instantiate(soapSplash, transform.position, Quaternion.identity);
                splash.GetComponent<NetworkObject>()?.Spawn();
                splash.transform.localScale = Vector3.one * size;
            }

            hasExploded = true;
            Collider[] explosionOverlaps = Physics.OverlapSphere(transform.position, radius * size, LayerMask.GetMask("Bubble", "Player"));
            Vector3 origin;
            Vector3 direction;
            foreach (Collider col in explosionOverlaps)
            {
                if (col == null) continue;
                origin = transform.position;
                direction = col.transform.position - transform.position;
                if (!Physics.Raycast(origin, direction, direction.magnitude, LayerMask.GetMask("Wall")))
                {
                    if (col.CompareTag("Player"))
                    {
                        PlayerController player = col.GetComponent<PlayerController>();
                        if (player != null)
                        {
                            if (GameManager.Instance.PlayingLocal)
                                player.ApplyKnockbackLocal(-1, direction, knockback * size, damage * size);
                            else
                                player.ApplyKnockbackServerRpc(-1, direction, knockback * size, damage * size);
                        }
                    }
                    else
                    {
                        BasicBubble bubble = col.GetComponent<BasicBubble>();
                        if (bubble != null)
                        {
                            bubble.BubbleCollision(this.gameObject);
                        }
                    }

                }
            }

            // Sound here
            if (IsServer)
            {
                NetworkObject.Despawn();
                Destroy(gameObject);
            }
        }
    }
    private void DestroyOnRestart()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        GameManager.Instance.OnGameStarted -= DestroyOnRestart;
    }
}
