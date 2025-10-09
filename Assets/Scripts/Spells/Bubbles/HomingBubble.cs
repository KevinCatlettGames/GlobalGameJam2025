using FMODUnity;
using UnityEngine;

public class HomingBubble : BasicBubble
{
    private HomingTargeting homingTargeting;
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float homingRadius = 5f;

    public override void InitialiseBubble(int ID, float dmg, float knb, float spd, float rng, float siz, float inf, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dmg, knb, spd, rng, siz, inf, dir, soundEvent, playerCollider);

        homingTargeting = GetComponentInChildren<HomingTargeting>();
        if (homingTargeting != null)
        {
            homingTargeting.SetTargeting(homingRadius / size, playerCollider);
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

        if (homingTargeting != null)
        {
            Vector3 targetVector = homingTargeting.GetTargetVector();

            if (targetVector != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(targetVector);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
            }
        }
        
        direction = transform.forward;

        base.BubbleMovement();
    }
}