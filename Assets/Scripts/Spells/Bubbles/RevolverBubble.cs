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
        if (!IsServer) return;

        OwnerID.Value = ID;
        damage = dmg;
        knockback = knb;
        speed = spd;
        range = rng;
        size.Value = siz;
        direction.Value = dir;
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
        Vector3 pos = transform.position + direction.Value;

        for (int i = 0; i < maxAmmo; i++)
        {
            float offset = (float)i - ((float)maxAmmo / 2f);
            Vector3 dir = Quaternion.AngleAxis(spread * offset, Vector3.up) * direction.Value;

            // Instantiate and network spawn the new bubble
            GameObject bubbleObj = Instantiate(bubblePrefab, pos, Quaternion.LookRotation(dir));
            NetworkObject netObj = bubbleObj.GetComponent<NetworkObject>();
            netObj.Spawn();

            BasicBubble bubbleScript = bubbleObj.GetComponent<BasicBubble>();
            bubbleScript.InitialiseBubble(OwnerID.Value, damage, knockback, speed, range, size.Value, inflationSpeed, dir, soundEvent, playerCollider);

            yield return new WaitForSeconds(delayBetweenShots);
        }

        NetworkObject.Despawn(true); // Despawn self after firing
    }
}
