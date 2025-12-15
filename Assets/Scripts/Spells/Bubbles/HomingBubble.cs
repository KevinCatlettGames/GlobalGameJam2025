using FMODUnity;
using System.Collections;
using UnityEngine;

public class HomingBubble : BasicBubble
{
    private HomingTargeting homingTargeting;
    [Header("Homing")]
    [SerializeField] private float rotationSpeed = 5f;
    [SerializeField] private float homingRadius = 5f;
    [Header("Trap")]
    [SerializeField] private float dmgMod = 1.5f;
    [SerializeField] private float knbMod = 2f;
    [SerializeField] private float spdMod = 2f;
    [SerializeField] private float sizMod = 1f;
    [SerializeField] private float growthRate = 2f;
    [SerializeField] private float stabRange = 10f;
    [SerializeField] private float stopDuration = 4f;
    [SerializeField] private Mesh stabMesh;
    private bool waitStage = false;
    private bool huntStage = false;
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
    protected override IEnumerator BubbleRangeLimit()
    {
        float lifetime = range / speed;
        yield return new WaitForSeconds(lifetime);
        waitStage = true;
        yield return new WaitForSeconds(stopDuration);
        StartCoroutine(StageSwitchCoroutine());
        yield return new WaitForSeconds(stopDuration);

        if (canMiss)
            IncrementMissedShotAchievement();

        Pop();
    }
    protected override void BubbleMovement()
    {
        if (!hasInflated)
        {
            base.BubbleMovement();
            return;
        }

        if (homingTargeting != null && waitStage)
        {
            Vector3 targetVector = homingTargeting.GetTargetVector();

            if (targetVector != Vector3.zero)
            {
                EnterHuntStage(targetVector);
                Quaternion targetRotation = Quaternion.LookRotation(targetVector);
                transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.fixedDeltaTime * rotationSpeed);
                direction = transform.forward;
            }
            else
            {
                if (huntStage)
                {
                    base.BubbleMovement();
                }
                else
                {
                    return;
                }
            }
        }
        base.BubbleMovement();
    }
    private IEnumerator HuntSwitchCoroutine()
    {
        float lifetime = stabRange / speed;
        yield return new WaitForSeconds(lifetime);
        if (canMiss)
            IncrementMissedShotAchievement();
        Pop();
    }
    private void EnterHuntStage(Vector3 target)
    {
        if (huntStage) return;
        huntStage = true;
        GetComponent<MeshFilter>().mesh = stabMesh;
        transform.rotation = Quaternion.LookRotation(target);
        StopCoroutine(rangeCoroutine);
        StartCoroutine(HuntSwitchCoroutine());
    }
    private IEnumerator StageSwitchCoroutine()
    {
        damage *= dmgMod;
        speed *= spdMod;
        knockback *= knbMod;
        float currentSize = size;
        size *= sizMod;
        do
        {
            currentSize += growthRate * Time.deltaTime;
            if (currentSize > size) 
                currentSize = size;
            transform.localScale = Vector3.one * currentSize;
            yield return null;
        } 
        while (currentSize < size);
    }
}