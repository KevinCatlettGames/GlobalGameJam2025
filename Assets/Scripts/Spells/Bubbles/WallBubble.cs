using FMODUnity;
using UnityEngine;

public class WallBubble : BasicBubble
{
    [Header("Special Stats")]
    [SerializeField] private float speedBosst = 1.5f;
    [SerializeField] private Material dmgedOutline;
    private int hitPoints = 0;
    private bool stop = false;

    public override void InitialiseBubble(int ID, Vector3 dir, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dir, playerCollider);
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

            MeshRenderer renderer = GetComponent<MeshRenderer>();
            Material[] materials = renderer.materials;
            materials[1] = dmgedOutline;
            renderer.materials = materials;

            other.GetComponent<BasicBubble>().ChangeSpeed(speedBosst);
            if (hitPoints <= 0)
            {
                Pop();
            }
        }
        else if (other.CompareTag("Wall"))
        {
            stop = true;
        }
    }

    protected override void BubbleMovement()
    {
        if(!stop)
            base.BubbleMovement();
    }
}