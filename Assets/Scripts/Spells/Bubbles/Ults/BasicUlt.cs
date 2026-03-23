using FMODUnity;
using UnityEngine;

public class BasicUlt : BasicBubble
{
    public override void InitialiseBubble(int ID, float dmg, float knb, float spd, float rng, float siz, float inf, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        //Manual Placeholders
        dmg = 50f;
        knb = 12f;
        spd = 25f;
        rng = 20f;
        siz = 10f;
        inf = 30f;
        base.InitialiseBubble(ID, dmg, knb, spd, rng, siz, inf, dir, soundEvent, playerCollider);
    }
}
