using FMODUnity;
using UnityEngine;

public class WallBubble : BasicBubble
{
    private int hitPoints = 0;

    public override void InitialiseBubble(int ID, float dmg, float knb, float spd, float rng, float siz, float inf, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dmg, knb, spd, rng, siz, inf, dir, soundEvent, playerCollider);
        Reflector reflector = GetComponent<Reflector>();
        if (reflector != null)
        {
            reflector.OwnerID = ID;
        }
        else
        {
            Debug.LogWarning("Reflector component missing on WallBubble.");
        }
        hitPoints = Mathf.Max(1, Mathf.RoundToInt(dmg));
    }

    protected override void BubbleMovement()
    {
    }

    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped || !IsServer) return;

        if (other.CompareTag("Player"))
        {
            return;
        }
        else if (other.CompareTag("Bubble"))
        {
            hitPoints--;
            if (hitPoints <= 0)
            {
                Pop();
            }
        }
    }
}