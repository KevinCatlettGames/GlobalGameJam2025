using FMODUnity;
using Unity.Netcode;
using UnityEngine;

public class HomingBubble : BasicBubble
{
    private HomingTargeting homingTargeting;
    [SerializeField] private float rotationSpeed;
    [SerializeField] private float homigRadius;

    public override void InitialiseBubble(int ID, float dmg, float knb, float spd, float rng, float siz, float inf, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dmg, knb, spd, rng, siz, inf, dir, soundEvent, playerCollider);

        homingTargeting = GetComponentInChildren<HomingTargeting>();
        if (homingTargeting != null)
        {
            homingTargeting.SetTargeting(homigRadius / size, playerCollider);
        }
        else
        {
            Debug.LogWarning("HomingTargeting component not found on HomingBubble.");
        }
    }

    protected override void BubbleMovement()
    {
        if (sphereCollider && !sphereCollider.enabled)
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
