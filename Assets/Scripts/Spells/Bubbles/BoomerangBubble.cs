using System.Collections;
using UnityEngine;

public class BoomerangBubble : BasicBubble
{
    [SerializeField] private float returnRangeMod = 1.2f;
    [SerializeField] private float knbMod = 1.5f;
    [SerializeField] private float spdMod = 1.5f;
    [SerializeField] private float dmgMod = 1.5f;
    [SerializeField] private float rotationAngle = 5f;
    [SerializeField] private float returnRotation = 2f;

    [SerializeField] private float stayDuration = 1f;
    [SerializeField] private float catchRange = 5f;

    protected override IEnumerator BubbleRangeLimit()
    {
        float lifetime = range / speed;
        float baseSpeed = speed;
        
        yield return new WaitForSeconds(lifetime);
        float angle = Vector3.SignedAngle(direction,playerCollider.transform.position - transform.position, Vector3.up);
        rotationAngle *= angle > 0 ? 1 : -1;        
        float timer = 0;
        do
        {
            Quaternion r = Quaternion.AngleAxis(rotationAngle * Time.deltaTime, Vector3.up);
            direction = r * direction;
            timer += Time.deltaTime;
            yield return null;
        } while (timer < stayDuration);
        ReturnRang(baseSpeed);
        lifetime *= returnRangeMod;
        Vector3 targetVector;
        timer = 0;
        do
        {
            timer += Time.deltaTime;
            if (!isReflected && playerCollider != null && playerCollider.enabled)
            {
                targetVector = playerCollider.transform.position - transform.position;
                if(targetVector.sqrMagnitude <= catchRange)
                    break;
                targetVector.y = 0;
                targetVector.Normalize();
                direction = Vector3.Lerp(direction, targetVector, Time.fixedDeltaTime * returnRotation);
            }
            yield return null;
        } while (timer < lifetime);

        if (canMiss)
            IncrementMissedShotAchievement();

        Pop();
    }
    private void ReturnRang(float baseSpeed)
    {
        //Effect Maybe
        knockback *= knbMod;
        damage *= dmgMod;
        speed = baseSpeed * spdMod;
    }
}

