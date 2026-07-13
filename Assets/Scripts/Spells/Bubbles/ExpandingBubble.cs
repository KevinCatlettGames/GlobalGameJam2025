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

    public override void InitialiseBubble(int ID, Vector3 dir, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dir, playerCollider);
        maxSize = size;
        size = startSize;
        knockbackRatio = knockback / maxSize;
        damageRatio = damage / maxSize;
    }

    private void Update()
    {
        // --- PREDICTION FILTER ---
        // Allow inflation if this is the Server OR a local client prediction fake
        if (!IsServer && !isLocalFake) return;

        if (sphereCollider != null && sphereCollider.enabled && currentSize < maxSize)
        {
            currentSize += inflationSpeed * Time.deltaTime;
            if (currentSize > maxSize) currentSize = maxSize;

            transform.localScale = Vector3.one * currentSize;
        }
    }

    protected override void BubbleMovement()
    {
        // --- PREDICTION FILTER ---
        // Allow movement processing for Server and client-side local fakes
        if (!IsServer && !isLocalFake) return;

        float currentSpeed = speed + currentSize * speedFactor;
        transform.position += direction * currentSpeed * Time.fixedDeltaTime;

        if (Vector3.Distance(transform.position, lastPosition) > desyncThreshold)
        {
            lastPosition = transform.position;
        }
    }

    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped || other == null) return;
        if (!IsServer && !isLocalFake) return;

        // --- LOCAL FAKE SEPARATION ---
        if (isLocalFake)
        {
            if (other.CompareTag("Player"))
            {
                // Visual pop instantly on player touch
                Pop();
            }
            else if (other.CompareTag("Bubble"))
            {
                // Fake bubble shrinks visually on hitting another bubble locally
                DamageBubble();
            }
            else
            {
                Pop();
            }
            return;
        }

        // --- AUTHORITATIVE SERVER LOGIC ---
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                if (GameManager.Instance.PlayingLocal)
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