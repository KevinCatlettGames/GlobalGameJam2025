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

    public override void InitialiseBubble(int ID, float dmg, float knb, float spd, float rng, float siz, float inf, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        OwnerID = ID;
        damage = dmg;
        knockback = knb;
        speed = spd;
        range = rng;
        size = siz;
        direction = dir;
        inflationSpeed = inf;
        this.soundEvent = soundEvent;
        this.playerCollider = playerCollider;
        StartCoroutine(EmptyBarrel());
    }

    protected override void BubbleMovement()
    {
        return;
    }
    private IEnumerator EmptyBarrel() 
    {
        Vector3 dir;
        Vector3 pos = transform.position + direction;
        BasicBubble bubbleScript;

        for (int i = 0; i < maxAmmo; i++) 
        {
            float f = (float)i - ((float)maxAmmo / 2f);
            dir = Quaternion.AngleAxis(spread * f, Vector3.up) * direction;
            bubbleScript = Instantiate(bubble, pos, Quaternion.LookRotation(dir)).GetComponent<BasicBubble>();
            bubbleScript.InitialiseBubble(OwnerID, damage, knockback, speed, range, size, inflationSpeed, dir, soundEvent, playerCollider);
            yield return new WaitForSeconds(delayBetweenShots);
        }

        Destroy(gameObject);
    }
}
