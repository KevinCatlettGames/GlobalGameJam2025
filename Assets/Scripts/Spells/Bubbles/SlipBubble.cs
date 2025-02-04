using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public class SlipBubble : BasicBubble
{
    [SerializeField] private GameObject slimeTrailObject;
    [SerializeField] private GameObject slimePuddleObject;
    private SlimeTrail slimeTrail;

    public override void InitialiseBubble(float dmg, float knb, float spd, float rng, float siz, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        base.InitialiseBubble(dmg, knb, spd, rng, siz, dir, soundEvent, playerCollider);
        GameObject trail = Instantiate(slimeTrailObject, new Vector3(transform.position.x, 0.06f, transform.position.z), Quaternion.LookRotation(transform.forward));
        slimeTrail = trail.GetComponent<SlimeTrail>();
        slimeTrail.InitialiseTrail(speed);
    }
    protected override void Pop()
    {
        slimeTrail.StopTrail();

        base.Pop();
    }
    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped) return;
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            player.ApplyKnockback(direction, knockback, damage);
            GameObject puddle = Instantiate(slimePuddleObject, new Vector3(transform.position.x, 0.06f, transform.position.z), Quaternion.LookRotation(transform.forward));
            puddle.gameObject.GetComponent<SlimeTrail>().StopTrail();
        }
        Pop();
    }
    public override void SetSlippy()
    {
        return;
    }
}
