using Febucci.UI.Core;
using System.Collections;
using UnityEngine;

public class BoomerangBubble : BasicBubble
{
    [SerializeField] private float returnRangeMod = 1.2f;
    [SerializeField] private float knbMod = 1.5f;
    [SerializeField] private float spdMod = 1.5f;
    [SerializeField] private float dmgMod = 1.5f;

    [SerializeField] private float stayDuration = 1f;
    [SerializeField] private float catchRange = 5f;

    protected override IEnumerator BubbleRangeLimit()
    {
        popOnBubbleHit = false;
        popOnPlayerHit = false;
        float lifetime = range / speed;
        float progress = 0;
        float baseSpeed = speed;
        
        yield return new WaitForSeconds(lifetime);
        speed = 0;
        popOnPlayerHit = true;
        popOnBubbleHit = true;
        yield return new WaitForSeconds(stayDuration);
        ReturnRang(baseSpeed);
        lifetime *= returnRangeMod;
        Vector3 targetVector;
        progress = 0;
        do
        {
            progress += Time.deltaTime;
            if (playerCollider != null && playerCollider.enabled)
            {
                targetVector = playerCollider.transform.position - transform.position;
                if(targetVector.sqrMagnitude <= catchRange)
                    break;
                targetVector.y = 0;
                targetVector.Normalize();
                direction = targetVector;
            }
            yield return null;
        } while (progress < lifetime);

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

