using Unity.Netcode;
using UnityEngine;

public class HomingBubble : BasicBubble
{
    private HomingTargeting homingTargeting;

    [Header("Homing")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float homingRadius = 5f;
    [SerializeField] private float secondDmgDelay = 0.25f;

    private PlayerController hitTarget;
    private float timeAlive = 0f;
    private bool achUnlocked = false;

    public override void InitialiseBubble(int ID, Vector3 dir, Collider playerCollider, int assignedSpellID, bool fakeWithServerCaster)
    {
        base.InitialiseBubble(ID, dir, playerCollider, assignedSpellID, fakeWithServerCaster);

        homingTargeting = GetComponentInChildren<HomingTargeting>();
        if (homingTargeting != null)
        {
            homingTargeting.SetTargeting(homingRadius / Mathf.Max(size, 0.01f), playerCollider, ID);
        }
        else
        {
            Debug.LogWarning($"[HomingBubble] HomingTargeting component missing on {gameObject.name}");
        }
    }

    protected override void BubbleMovement()
    {
        if (!hasInflated)
        {
            base.BubbleMovement();
            return;
        }

        // Steer toward target if homing component is present
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
        if (hasPopped) return;
        if (!IsServer && !isLocalFake) return;

        if (other != null && other.CompareTag("Player"))
        {
            hitTarget = other.GetComponent<PlayerController>();
        }

        base.BubbleCollision(other);
    }

    protected override bool DetectsImpact(out Vector3 impactPoint)
    {
        impactPoint = transform.position;

        // Cast ray dynamically in direction of current homing turn
        Vector3 moveDelta = direction * speed * Time.fixedDeltaTime;
        float moveDistance = moveDelta.magnitude;

        if (moveDistance <= 0.001f) return false;

        float checkRadius = (sphereCollider != null) ? (sphereCollider.radius * transform.localScale.x * 0.5f) : (currentSize * 0.5f);

        if (Physics.SphereCast(transform.position, checkRadius, direction.normalized, out RaycastHit hit, moveDistance + 0.05f))
        {
            if (ignoredColliders.Contains(hit.collider) || hit.collider.transform.root == transform.root)
                return false;

            impactPoint = hit.point;
            return true;
        }

        return false;
    }

    [ClientRpc]
    protected override void SpawnPopEffectClientRpc(Vector3 pos)
    {
        // Suppress duplicate visual instantiation on local predicted objects
        if (!IsServer && isLocalFake) return;

        if (fizzleEffect == null) return;

        GameObject vfx = Instantiate(fizzleEffect, pos, Quaternion.identity);

        if (hitTarget != null)
        {
            vfx.GetComponent<DamageAfterDelay>()?.StartDamageAfterDelay(hitTarget, OwnerID.Value, damage, secondDmgDelay);
        }
    }
}