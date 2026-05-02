using FMODUnity;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class GrenadeBubble : BasicBubble
{
    private bool hasExploded = false;
    [Header("Special Stats")]
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float explosionDamage = 5f;
    [SerializeField] private float explosionKnockback = 5f;
    [SerializeField] private float vulnerableDuration = 4f;
    [SerializeField] private AnimationCurve arc;
    [SerializeField] private GameObject splat;
    [SerializeField] private LayerMask groundedLayerMask;
    private float progress = 0f;
    private const float raycastDistance = 5f;

    public override void InitialiseBubble(int ID, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dir, soundEvent, playerCollider);
        canMiss = false;
    }
    protected override void BubbleMovement()
    {
        progress += speed * Time.fixedDeltaTime;
        transform.position = new Vector3(transform.position.x, arc.Evaluate(progress / range), transform.position.z);
        base.BubbleMovement();
        if (transform.position.y <= 0.1f)
        {
            transform.position = new Vector3(transform.position.x, 0.1f, transform.position.z);
            Pop();
        }
    }
    protected override void Pop()
    {
        if (hasInflated)
            Explode();       
        base.Pop();
    }
    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;
        fizzleEffect = hitEffect;
        Collider[] explosionOverlaps = Physics.OverlapSphere(transform.position, explosionRadius, LayerMask.GetMask("Bubble", "Player"));
        Vector3 origin;
        Vector3 direction;
        foreach (Collider col in explosionOverlaps)
        {
            if (!col || col.gameObject == gameObject) continue;
            origin = transform.position;
            direction = col.transform.position - transform.position;
            if (!Physics.Raycast(origin, direction, direction.magnitude, LayerMask.GetMask("Wall")))
            {
                if (col.CompareTag("Player"))
                {
                    GameManager gameManager = GameManager.Instance;
                    
                    PlayerController player = col.GetComponent<PlayerController>();
                    if (player != null)
                    {
                        if (gameManager.PlayingLocal)
                            player.ApplyKnockbackLocal(OwnerID, direction, explosionKnockback, explosionDamage);
                        else
                            player.ApplyKnockbackServerRpc(OwnerID, direction, explosionKnockback, explosionDamage);
                        
                        gameManager.ChangeHitReference(OwnerID, spellType, player.PlayerID, isSoaped, isReflected);
                        player.StartVulnerable(vulnerableDuration);
                        playerCollider.GetComponent<PlayerController>().GainUltCharge(explosionDamage, true);
                    }
                }
                else
                {
                    BasicBubble bubble = col.GetComponent<BasicBubble>();
                    if (bubble != null)
                    {
                        bubble.BubbleCollision(gameObject);
                    }
                }

            }
        }
        if (Physics.Raycast(new Vector3(transform.position.x, 2f, transform.position.z), Vector3.down, out RaycastHit hitInfo, raycastDistance, groundedLayerMask))
        {
            if (IsServer)
            {
                GameObject puddle;
                puddle = Instantiate(splat, hitInfo.point, transform.rotation);
                puddle.GetComponent<NetworkObject>()?.Spawn();
                puddle.GetComponent<DamageField>()?.SetID(OwnerID);
            }
        }
    }
}
