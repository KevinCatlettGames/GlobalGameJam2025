using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlipBubble : BasicBubble
{
    [SerializeField] private GameObject slimeTrailObject;
    private SlimeTrail slimeTrail;

    public override void InitialiseBubble(float dmg, float knb, float spd, float rng, float siz, Vector3 dir)
    {
        base.InitialiseBubble(dmg, knb, spd, rng, siz, dir);
        GameObject trail = Instantiate(slimeTrailObject, new Vector3(transform.position.x, 0.06f, transform.position.z), Quaternion.LookRotation(transform.forward));
        slimeTrail = trail.GetComponent<SlimeTrail>();
        slimeTrail.InitialiseTrail(speed);
    }
    protected override void Pop()
    {
        slimeTrail.StopTrail();
        base.Pop();
    }
}
