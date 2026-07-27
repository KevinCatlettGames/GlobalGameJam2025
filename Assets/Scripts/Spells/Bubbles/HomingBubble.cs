using Unity.Netcode;
using UnityEngine;

public class HomingBubble : BasicBubble
{
    private HomingTargeting homingTargeting;
    [Header("Homing")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float homingRadius = 5f;
    [SerializeField] private float secondDmgDelay = .25f;
    private PlayerController hitTarget;

    public override void InitialiseBubble(int ID, Vector3 dir, Collider playerCollider, int assignedSpellID, bool fakeWithServerCaster)
    {
        base.InitialiseBubble(ID, dir, playerCollider, assignedSpellID, fakeWithServerCaster);

        homingTargeting = GetComponentInChildren<HomingTargeting>();
        if (homingTargeting != null)
        {
            homingTargeting.SetTargeting(homingRadius / size, playerCollider, ID);
        }
        else
        {
            Debug.LogWarning("HomingTargeting component not found on HomingBubble.");
        }
    }
    protected override void BubbleMovement()
    {
        if (!hasInflated)
        {
            base.BubbleMovement();
            return;
        }

        Vector3 targetVector = homingTargeting.GetTargetVector();

        if (targetVector != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetVector);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
            direction = transform.forward;
        }

        base.BubbleMovement();
    }
    public override void BubbleCollision(GameObject other)
    {
        if (other.CompareTag("Player"))
            hitTarget = other.GetComponent<PlayerController>();        

        base.BubbleCollision(other);
    }

    [ClientRpc]
    protected override void SpawnPopEffectClientRpc(Vector3 pos)
    {
        if (!IsServer && GameManager.Instance.Players[OwnerID.Value].IsOwner) return;

        if (fizzleEffect == null) return;
        GameObject fx = Instantiate(fizzleEffect, pos, Quaternion.identity);
        if (fx != null && hitTarget != null)
        {
            fx.GetComponent<DamageAfterDelay>()?.StartDamageAfterDelay(hitTarget, OwnerID.Value, damage, secondDmgDelay);
        }
    }
}