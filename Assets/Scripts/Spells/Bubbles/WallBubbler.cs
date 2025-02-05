using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WallBubbler : BasicBubble
{
    private EventReference soundEvent;
    [SerializeField] private int wallSegents = 3;
    [SerializeField] GameObject bubble;
    private float segmentDistance;
    public override void InitialiseBubble(float dmg, float knb, float spd, float rng, float siz, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        damage = dmg;
        knockback = knb;
        speed = spd;
        range = rng;
        size = siz;
        direction = dir;
        RuntimeManager.PlayOneShotAttached(soundEvent,gameObject);
        segmentDistance = siz * 1.2f;
        StartCoroutine(MakeWall());
    }

    protected override void BubbleMovement()
    {
        return;
    }
    private IEnumerator MakeWall()
    {
        Vector3 pos;
        Vector3 offset = Vector3.Cross(direction,Vector3.up).normalized;
        offset *= segmentDistance;
        BasicBubble bubbleScript;

        for (int i = 0; i < wallSegents; i++)
        {
            pos = transform.position + direction + offset * (i - 2);
            bubbleScript = Instantiate(bubble, pos, Quaternion.LookRotation(direction)).GetComponent<BasicBubble>();
            bubbleScript.InitialiseBubble(damage, knockback, speed, range, size, direction, soundEvent, null);
            yield return new WaitForSeconds(0.04f);
        }

        Destroy(gameObject);
    }
}
