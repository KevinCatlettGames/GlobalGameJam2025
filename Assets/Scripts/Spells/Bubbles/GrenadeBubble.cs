using FMODUnity;
using System.Collections;
using UnityEngine;

public class GrenadeBubble : BasicBubble
{
    private bool hasExploded = false;
    [Header("Special Stats")]
    [SerializeField] private float explosionRadius = 5f;
    [SerializeField] private float vulnerableDuration = 4f;
    [SerializeField] private AnimationCurve arc;
    private float evaluateStep = 1f;
    private float progress = 0f;

    public override void InitialiseBubble(int ID, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dir, soundEvent, playerCollider);
        canMiss = false;
        evaluateStep = range / speed;
    }
    protected override void BubbleMovement()
    {
        progress += evaluateStep * Time.fixedDeltaTime;
        transform.position = new Vector3(transform.position.x, arc.Evaluate(progress), transform.position.z);
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
                        playerCollider.GetComponent<PlayerController>().GainUltCharge(damage, true);
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
