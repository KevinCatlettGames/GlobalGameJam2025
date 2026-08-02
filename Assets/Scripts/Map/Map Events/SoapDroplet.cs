using FMODUnity;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class SoapDroplet : NetworkBehaviour
{
    [SerializeField] private GameObject soapSplash;
    [SerializeField] private GameObject dangerVFX;
    [SerializeField] private float startDelay = .5f;
    [SerializeField] private EventReference soundEvent;
    [SerializeField] private EventReference splatEvent;
    
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
        ResetDroplet();
        GameManager.Instance.OnGameEnded += ResetOnRoundEnd;
    }

    public void ActivateDroplet(Vector3 position, float time)
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && IsServer)
            ActivateDropletServerRpc(position, time);
        else
        {
            gameObject.SetActive(true);
            size = Random.Range(minSize, maxSize);
            transform.localScale = Vector3.one * size;
            transform.position = position;
            float delay = Random.Range(0f, time);
            StartCoroutine(Fall(delay));
        }
    }

    [ServerRpc]
    void ActivateDropletServerRpc(Vector3 position, float time)
    {
        size = Random.Range(minSize, maxSize);
        float delay = Random.Range(0f, time);
        ActivateDropletClientRpc(position, delay, size);
    }

    [ClientRpc]
    void ActivateDropletClientRpc(Vector3 position, float delay, float size)
    {
        this.size = size;
        gameObject.SetActive(true);
        transform.localScale = Vector3.one * size;
        transform.position = position;
        StartCoroutine(Fall(delay));
    }

    private IEnumerator Fall(float delay)
    {
        yield return new WaitForSeconds(delay);
        dropletTransform.gameObject.SetActive(true);
        RuntimeManager.PlayOneShotAttached(soundEvent, gameObject);
        float fallSpeed = dropletFallSpeed;
        GameObject effect = Instantiate(dangerVFX, transform.position, Quaternion.identity);
        effect.transform.localScale = Vector3.one * size;
        
        while (dropletTransform.position.y > 0)
        {
            fallSpeed += Time.deltaTime * gravity;
            dropletTransform.position = dropletTransform.position + fallSpeed * Time.deltaTime * Vector3.down;
            yield return null;
        }

        if (IsServer)
        {
            GameObject splash = Instantiate(soapSplash, transform.position, Quaternion.identity);
            splash.transform.localScale = Vector3.one * size;
            splash.GetComponent<NetworkObject>()?.Spawn();
        }

        RuntimeManager.PlayOneShotAttached(splatEvent, gameObject);

        if (IsServer)
        {
            Collider[] explosionOverlaps = Physics.OverlapSphere(transform.position, radius * size, LayerMask.GetMask("Bubble", "Player", "LocalPlayer"));
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
        }
        Destroy(effect);
        ResetDroplet();
    }
    private void ResetDroplet()
    {
        dropletTransform.position = new Vector3 (dropletTransform.position.x, startHeight, dropletTransform.position.z);
        dropletTransform.gameObject.SetActive(false);
        gameObject.SetActive(false);
    }
    private void ResetOnRoundEnd()
    {
        ResetDroplet();
    }

    private void OnDestroy()
    {
        GameManager.Instance.OnGameEnded -= ResetOnRoundEnd;
    }
}
