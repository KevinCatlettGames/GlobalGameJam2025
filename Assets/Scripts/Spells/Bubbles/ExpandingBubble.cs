using FMODUnity;
using System.Collections;
using UnityEngine;

public class ExpandingBubble : BasicBubble
{
    [SerializeField] private float startSize = 1f;
    [SerializeField] private float sizeLossOnHit = 1.5f;
    [SerializeField] private float speedFactor = .5f;
    private float knockbackRatio = 1f;
    private float damageRatio = 1f;
    private float maxSize = 1f;

    public override void InitialiseBubble(int ID, float dmg, float knb, float spd, float rng, float siz, float inf, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dmg, knb, spd, rng, siz, inf, dir, soundEvent, playerCollider);
        maxSize = size;
        size = startSize;
        knockbackRatio = knockback / maxSize;
        damageRatio = damage / maxSize;
    }
    private void Update()
    {
        if (!IsServer) return;
              
        if (sphereCollider.enabled && currentSize < maxSize)
        {
            currentSize += inflationSpeed * Time.deltaTime;
            if (currentSize > maxSize) currentSize = maxSize;

            transform.localScale = Vector3.one * currentSize;
        }
    }
    protected override void BubbleMovement()
    {
        if (!IsServer) return;

        float currentSpeed = speed + currentSize * speedFactor;
        transform.position += direction * currentSpeed * Time.fixedDeltaTime;

        if (Vector3.Distance(transform.position, lastPosition) > desyncThreshold)
        {
            lastPosition = transform.position;
        }
    }
    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped || !IsServer) return;

        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                if (GameManager.Instance.playingLocal)
                    player.ApplyKnockbackLocal(OwnerID, direction, knockbackRatio * currentSize, damageRatio * currentSize);
                else
                    player.ApplyKnockbackServerRpc(OwnerID, direction, knockbackRatio * currentSize, damageRatio * currentSize);
            }
            Pop();
        }
        else if (other.CompareTag("Bubble"))
        {
            DamageBubble();
        }
        else
        {
            Pop();
        }
    }
    private void DamageBubble()
    {
        currentSize -= sizeLossOnHit;
        if (currentSize <= .5f)       
            Pop();       
        else
            transform.localScale = Vector3.one * currentSize;
    }
}