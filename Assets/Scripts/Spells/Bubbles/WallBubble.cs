using FMODUnity;
using UnityEngine;

public class WallBubble : BasicBubble
{
    [Header("Special Stats")]
    [SerializeField] private float speedBosst = 1.5f;
    private int hitPoints = 0;

    public override void InitialiseBubble(int ID, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dir, soundEvent, playerCollider);
        Reflector reflector = GetComponent<Reflector>();
        if (reflector != null)
        {
            reflector.OwnerID = ID;
        }
        else
        {
            Debug.LogWarning("Reflector component missing on WallBubble.");
        }

        canMiss = false;
        hitPoints = Mathf.Max(1, Mathf.RoundToInt(damage));
    }
    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped || !IsServer) return;

        if (other.CompareTag("Player"))
        {
            return;
        }
        else if (other.CompareTag("Bubble") && popOnBubbleHit)
        {
            hitPoints--;
            other.GetComponent<BasicBubble>().IncreaseSpeed(speedBosst);
            if (hitPoints <= 0)
            {
                Pop();
            }
        }
        else if (other.CompareTag("Wall"))
        {
            Pop();
        }
    }
}