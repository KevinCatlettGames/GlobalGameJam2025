using FMODUnity;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class HomingBubble : BasicBubble
{
    private HomingTargeting homingTargeting;
    [Header("Homing")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float homingRadius = 5f;
    [SerializeField] private float damageDelay = .01f;
    private PlayerController playerHit;

    public override void InitialiseBubble(int ID, Vector3 dir, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dir, playerCollider);

        homingTargeting = GetComponentInChildren<HomingTargeting>();
        if (homingTargeting != null)
        {
            // Allow targeting setups to be initialized for both server and local client prediction fakes
            homingTargeting.SetTargeting(homingRadius / size, playerCollider, ID);
        }
        else
        {
            Debug.LogWarning("HomingTargeting component not found on HomingBubble.");
        }
    }

    protected override void BubbleMovement()
    {
        // --- PREDICTION FILTER ---
        // Allow movement processing for both the Server and our client-side prediction fake
        if (!IsServer && !isLocalFake) return;

        if (!hasInflated)
        {
            base.BubbleMovement();
            return;
        }

        // Run homing track math locally so the fake bubble bends gracefully on screen
        if (homingTargeting != null)
        {
            Vector3 targetVector = homingTargeting.GetTargetVector();

            if (targetVector != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetVector);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
                direction = transform.forward;
            }
        }

        base.BubbleMovement();
    }

    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped || other == null) return;
        if (!IsServer && !isLocalFake) return;

        if (other.CompareTag("Player"))
        {
            playerHit = other.GetComponent<PlayerController>();
        }

        if (isLocalFake)
        {
            // Visual local fakes pop cleanly upon intersecting any collider
            Pop();
            return;
        }

        base.BubbleCollision(other);
    }

    [ClientRpc]
    protected override void SpawnPopEffectClientRpc(Vector3 pos)
    {
        if (fizzleEffect == null) return;

        var effect = Instantiate(fizzleEffect, pos, Quaternion.identity);

        // This authoritative damage pipeline remains totally safe on connected network game clients
        if (playerHit != null && hasHitPlayer)
        {
            effect.GetComponent<DamageAfterDelay>()?.StartDamageAfterDelay(playerHit, OwnerID, damage, damageDelay);
        }
    }
}