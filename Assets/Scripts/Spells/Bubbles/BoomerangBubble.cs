using Febucci.UI.Core;
using System.Collections;
using UnityEngine;

public class BoomerangBubble : BasicBubble
{
    [SerializeField] private AnimationCurve speedCurve;
    [SerializeField] private float returnRangeMod = 1.2f;
    [SerializeField] private float knbMod = 1.5f;
    [SerializeField] private float spdMod = 1.5f;
    [SerializeField] private float dmgMod = 1.5f;
    [SerializeField] private float startAngle = 5f;
    [SerializeField] private float turnRate = .1f;

    protected override IEnumerator BubbleRangeLimit()
    {
        float lifetime = range / speed;
        float f = 1f / lifetime;
        float progress = 0;
        float baseSpeed = speed;
        direction = direction.RotateAround(new Vector2(0,0),startAngle);
        do
        {
            progress += f * Time.deltaTime;
            speed = baseSpeed * speedCurve.Evaluate(progress);
            yield return null;
        } while (progress < 1);
        ReturnRang();
        lifetime *= returnRangeMod;
        f = 1f / lifetime;
        do
        {
            progress -= f * Time.deltaTime;
            speed = baseSpeed * speedCurve.Evaluate(progress) * spdMod;
            yield return null;
        } while (progress > 0);

        if (canMiss)
            IncrementMissedShotAchievement();

        Pop();
    }
    private void ReturnRang()
    {
        //Effect Maybe
        knockback *= knbMod;
        damage *= dmgMod;
        Vector3 targetDirection = playerCollider.transform.position - transform.position;
        targetDirection += playerCollider.GetComponent<CharacterController>().velocity * startAngle;
        direction = targetDirection;
        direction.Normalize();
        Quaternion rotation = Quaternion.LookRotation(direction);
        transform.rotation = rotation;
    }
}

