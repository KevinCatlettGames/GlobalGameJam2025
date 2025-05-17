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
        homingTargeting.SetTargeting(homigRadius / size, playerCollider);
    }
    protected override void BubbleMovement()
    {
        if (!sphereCollider.enabled)
        {
            base.BubbleMovement(); 
            return;
        }
        Vector3 targetVector = homingTargeting.GetTargetVector();
        if (targetVector != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetVector);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
        }

        direction = transform.forward;
        base.BubbleMovement();
    }
}
