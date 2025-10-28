using FMODUnity;
using System.Collections;
using UnityEngine;

public class HomingBubble : BasicBubble
{
    private HomingTargeting homingTargeting;
    [Header("Homing")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float homingRadius = 5f;
    [Header("Stab")]
    [SerializeField] private float dmgMod = 1.5f;
    [SerializeField] private float knbMod = 2f;
    [SerializeField] private float speedMod = 2f;
    [SerializeField] private float rotMod = 1.5f;
    [SerializeField] private float stabRange = 10f;
    [SerializeField] private float stopDuration = .1f;
    [SerializeField] private Mesh stabMesh;
    private float homingDuration = 0f;
    private float stabSpeed = 0f;
    private bool stabStage = false;
    public override void InitialiseBubble(int ID, float dmg, float knb, float spd, float rng, float siz, float inf, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        homingDuration = rng / spd;
        rng += stabRange;
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
        StartCoroutine(StageSwitchCoroutine());
    }

    protected override void BubbleMovement()
    {
        if (!hasInflated)
        {
            base.BubbleMovement();
            return;
        }

        if (homingTargeting != null && !stabStage)
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
    private IEnumerator StageSwitchCoroutine()
    {
        yield return new WaitForSeconds(homingDuration);
        stabSpeed = speed * speedMod;
        speed = 0;
        rotationSpeed *= rotMod;
        GetComponent<MeshFilter>().mesh = stabMesh;
        yield return new WaitForSeconds(stopDuration);
        EnterStabStage();
    }
    private void EnterStabStage()
    {
        stabStage = true;
        damage *= dmgMod;
        speed = stabSpeed;
        knockback *= knbMod;
    }
}