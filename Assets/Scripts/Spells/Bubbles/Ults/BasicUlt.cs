using FMODUnity;
using UnityEngine;

public class BasicUlt : BasicBubble
{
    public override void InitialiseBubble(int ID, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dir, soundEvent, playerCollider);
    }
}
