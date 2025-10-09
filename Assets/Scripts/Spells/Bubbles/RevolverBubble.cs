using FMODUnity;
using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class RevolverBubble : BasicBubble
{
    [SerializeField] private int maxAmmo = 6;
    [SerializeField] private float delayBetweenShots = 0.02f;
    [SerializeField] private float spread = 2f;
    [SerializeField] private GameObject bubblePrefab;

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
        // Revolver bubble doesn't move itself
    }

    private IEnumerator EmptyBarrel()
    {
        Vector3 pos = transform.position + direction;
        float rotation = -(maxAmmo - 1);
        rotation *= .5f;
        
        for (int i = 0; i < maxAmmo; i++) 
        {
            
                Vector3 dir = Quaternion.AngleAxis(spread * rotation, Vector3.up) * direction;
    
                GameObject bubbleObj = Instantiate(bubblePrefab, pos, Quaternion.LookRotation(dir));
    
                NetworkObject netObj = bubbleObj.GetComponent<NetworkObject>();
                if (netObj != null)
                    netObj.Spawn();

                BasicBubble bubbleScript = bubbleObj.GetComponent<BasicBubble>();
                bubbleScript.InitialiseBubble(OwnerID, damage, knockback, speed, range, size, inflationSpeed, dir, soundEvent, playerCollider);

                yield return new WaitForSeconds(delayBetweenShots);
                rotation++;
            
        }
        yield return new WaitForSeconds(1f);
        Destroy(gameObject);
    }
}
