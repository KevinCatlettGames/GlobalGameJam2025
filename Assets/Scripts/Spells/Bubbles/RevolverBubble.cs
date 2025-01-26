using FMODUnity;
using System.Collections;
using UnityEngine;
public class RevolverBubble : BasicBubble
{
    [SerializeField] private int maxAmmo = 6;
    [SerializeField] private float delayBetweenShots = 0.02f;
    [SerializeField] private float spread = 2f;
    [SerializeField] GameObject bubble;
    private EventReference soundEvent;

    public override void InitialiseBubble(float dmg, float knb, float spd, float rng, float siz, Vector3 dir, EventReference soundEvent)
    {
        damage = dmg;
        knockback = knb;
        speed = spd;
        range = rng;
        size = siz;
        direction = dir;
        this.soundEvent = soundEvent;
        StartCoroutine(EmptyBarrel());
    }

    protected override void BubbleMovement()
    {
        return;
    }
    private IEnumerator EmptyBarrel() 
    {
        Vector3 dir;
        Vector3 pos = transform.position;
        BasicBubble bubbleScript;

        for (int i = 0; i < maxAmmo; i++) 
        {
            float f = (float)i - ((float)maxAmmo / 2f);
            dir = Quaternion.AngleAxis(spread * f, Vector3.up) * direction;
            bubbleScript = Instantiate(bubble, pos, Quaternion.LookRotation(dir)).GetComponent<BasicBubble>();
            bubbleScript.InitialiseBubble(damage, knockback, speed, range, size, dir, soundEvent);
            yield return new WaitForSeconds(delayBetweenShots);
        }

        Destroy(gameObject);
    }
}
