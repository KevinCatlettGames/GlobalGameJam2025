using System.Collections;
using FMODUnity;
using Unity.Netcode;
using UnityEngine;

public class SlashBubble : BasicBubble
{
    [Header("SpecialStats")]
    [SerializeField] private GameObject slasherL;
    [SerializeField] private GameObject slasherR;
    [SerializeField] private Transform spinner;

    public override void InitialiseBubble(int ID, Vector3 dir, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dir, playerCollider);
        canMiss = false;

        // Ensure coroutines fire off for both the authoritative server instance and local visual fakes
        StartCoroutine(StartSlashers());
    }

    private IEnumerator StartSlashers()
    {
        while (currentSize < size)
        {
            currentSize += inflationSpeed * Time.deltaTime;
            if (currentSize > size) currentSize = size;

            if (slasherL != null) slasherL.transform.localScale = Vector3.one * currentSize;
            if (slasherR != null) slasherR.transform.localScale = Vector3.one * currentSize;
            yield return null;
        }

        // Initialize sub-slasher configuration parameters safely
        if (slasherL != null) slasherL.GetComponentInChildren<Slasher>()?.SetInflated(playerCollider, OwnerID);
        if (slasherR != null) slasherR.GetComponentInChildren<Slasher>()?.SetInflated(playerCollider, OwnerID);

        hasInflated = true;
    }

    protected override void BubbleMovement()
    {
        // Allow tracking of casting player anchor position across both execution contexts
        if (playerCollider != null)
        {
            transform.position = playerCollider.transform.position;
        }
    }

    protected override IEnumerator BubbleRangeLimit()
    {
        while (!hasInflated)
            yield return null;

        float rotation = 0f;
        float angle = 0f;
        while (Mathf.Abs(rotation) < range)
        {
            angle = speed * Time.deltaTime;
            transform.Rotate(Vector3.up * angle);
            rotation += angle;
            yield return null;
        }

        Pop();
    }

    public void SlasherHit(Vector3 slasherDir, GameObject other)
    {
        if (other == null) return;
        if (!IsServer && !isLocalFake) return; // Ignore unmanaged remote proxy scripts

        direction = slasherDir;

        // --- 1. REFLECTION HANDLING (Shared logic to keep rotations synchronized) ---
        if (other.TryGetComponent<Reflector>(out var reflector) && reflector.GetIsReflecting())
        {
            if (IsServer)
            {
                OwnerID = reflector.OwnerID;
            }
            Reflect(Vector3.zero);
            return;
        }

        // --- 2. LOCAL FAKE SHORT CIRCUIT ---
        if (isLocalFake)
        {
            // If the local melee fake intersects an enemy projectile, simulate a clash visual pop on it instantly
            if (other.CompareTag("Bubble") && other.TryGetComponent<BasicBubble>(out BasicBubble clientBubble))
            {
                if (clientBubble.OwnerID != OwnerID)
                {
                    // Trigger dynamic local popping simulation behavior on opposing client projectile
                    clientBubble.BubbleCollision(gameObject);
                }
            }
            return;
        }

        // --- 3. AUTHORITATIVE SERVER LOGIC ---
        if (other.CompareTag("Bubble") && other.TryGetComponent<BasicBubble>(out BasicBubble serverBubble))
        {
            if (serverBubble.OwnerID != OwnerID)
            {
                serverBubble.BubbleCollision(gameObject);
            }
        }

        BubbleCollision(other);
    }

    protected override void Reflect(Vector3 normal)
    {
        if (isReflected) return;
        isReflected = !isReflected;
        speed *= -1f; // Inverts the spin direction natively for client-side matching!

        if (rangeCoroutine != null)
            StopCoroutine(rangeCoroutine);

        rangeCoroutine = StartCoroutine(BubbleRangeLimit());
    }
}