using FMODUnity;
using Unity.Netcode;
using UnityEngine;

public class WallBubble : BasicBubble
{
    [Header("Special Stats")]
    [SerializeField] private float speedBoost = 1.5f; // Fixed typo in variable name from speedBosst
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
        if (hasPopped || other == null) return;
        if (!IsServer && !isLocalFake) return; // Process for authoritative server instances and client fakes

        // --- 1. SHARED COLLISION INTERACTIONS (Players always pass through safely) ---
        if (other.CompareTag("Player"))
        {
            return;
        }

        // --- 2. LOCAL FAKE SHORT CIRCUIT ---
        if (isLocalFake)
        {
            if (other.CompareTag("Wall") || other.CompareTag("Environment"))
            {
                stop = true; // Freeze the fake shield position locally instantly on the wall
            }
            else if (other.CompareTag("Bubble") && popOnBubbleHit)
            {
                // Update material feedback locally so the casting player sees immediate impact response
                if (GetComponent<MeshRenderer>() != null && dmgedOutline != null)
                {
                    MeshRenderer renderer = GetComponent<MeshRenderer>();
                    Material[] materials = renderer.materials;
                    if (materials.Length > 1)
                    {
                        materials[1] = dmgedOutline;
                        renderer.materials = materials;
                    }
                }
            }
            return;
        }

        // --- 3. AUTHORITATIVE SERVER COLLISION DETECTION ---
        if (other.CompareTag("Bubble") && popOnBubbleHit)
        {
            hitPoints--;

            MeshRenderer renderer = GetComponent<MeshRenderer>();
            if (renderer != null && dmgedOutline != null)
            {
                Material[] materials = renderer.materials;
                if (materials.Length > 1)
                {
                    materials[1] = dmgedOutline;
                    renderer.materials = materials;
                }
            }

            BasicBubble otherBubble = other.GetComponent<BasicBubble>();
            if (otherBubble != null)
            {
                otherBubble.ChangeSpeed(speedBoost);
            }

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
        // Allow tracking movement frames for both server logic and local preview fakes
        if (!IsServer && !isLocalFake) return;

        if (!stop)
            base.BubbleMovement();
    }
}