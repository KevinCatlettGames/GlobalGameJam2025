using FMODUnity;
using UnityEngine;

public class GrenadeBubble : BasicBubble
{
    private bool hasExploded = false;
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float height = 20f;
    [SerializeField] private float gravity = 10f;
    [SerializeField] private float vulnerableDuration = 4f;

    public override void InitialiseBubble(int ID, float dmg, float knb, float spd, float rng, float siz, float inf, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        dir.y = height;
        base.InitialiseBubble(ID, dmg, knb, spd, rng, siz, inf, dir, soundEvent, playerCollider);
        canMiss = false;
    }
    protected override void BubbleMovement()
    {
        direction.y -= gravity * Time.fixedDeltaTime;
        transform.rotation = Quaternion.LookRotation(direction);
        if (transform.position.y <= 0)
        {
            Pop();
        }
        base.BubbleMovement();
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
            if (col == null || col.gameObject == gameObject) continue;
            origin = transform.position;
            direction = col.transform.position - transform.position;
            //Debug.Log(col.name);
            if (!Physics.Raycast(origin, direction, direction.magnitude, LayerMask.GetMask("Wall")))
            {
                if (col.CompareTag("Player"))
                {
                    GameManager gameManager = GameManager.Instance;
                    
                    PlayerController player = col.GetComponent<PlayerController>();
                    if (player != null)
                    {
                        if (gameManager.PlayingLocal)
                            player.ApplyKnockbackLocal(OwnerID, direction, knockback, damage);
                        else
                            player.ApplyKnockbackServerRpc(OwnerID, direction, knockback, damage);
                        
                        gameManager.ChangeHitReference(OwnerID, spellType, player.PlayerID, isSoaped, isReflected);
                        player.StartVulnerable(vulnerableDuration);
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
    }
}
