using System.Collections;
using FMODUnity;
using Unity.Netcode;
using UnityEngine;

public class SlashBubble : BasicBubble
{
    [Header("Special Stats")]
    [SerializeField] private GameObject slasherL;
    [SerializeField] private GameObject slasherR;
    [SerializeField] private Transform spinner;

    public override void InitialiseBubble(int ID, Vector3 dir, Collider playerCollider, int assignedSpellID, bool fakeWithServerCaster)
    {
        base.InitialiseBubble(ID, dir, playerCollider, assignedSpellID, fakeWithServerCaster);
        canMiss = false;

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

        if (slasherL != null) slasherL.GetComponentInChildren<Slasher>()?.SetInflated(playerCollider, OwnerID.Value);
        if (slasherR != null) slasherR.GetComponentInChildren<Slasher>()?.SetInflated(playerCollider, OwnerID.Value);

        hasInflated = true;
    }

    protected override void BubbleMovement()
    {
        if (!IsServer && !isLocalFake) return;

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
            angle = speed * Time.fixedDeltaTime;
            transform.Rotate(Vector3.up * angle);
            rotation += angle;
            yield return new WaitForFixedUpdate();
        }

        Pop();
    }

    public void SlasherHit(Vector3 slasherDir, GameObject other)
    {
        if (other == null) return;
        if (!IsServer && !isLocalFake) return;

        direction = slasherDir;

        if (other.TryGetComponent<Reflector>(out var reflector) && reflector.GetIsReflecting())
        {
            if (IsServer)
            {
                OwnerID.Value = reflector.OwnerID;
            }

            Reflect(Vector3.zero);
            return;
        }

        if (isLocalFake)
        {
            if (other.CompareTag("Bubble") && other.TryGetComponent<BasicBubble>(out BasicBubble clientBubble))
            {
                if (clientBubble.OwnerID.Value != OwnerID.Value)
                {
                    clientBubble.BubbleCollision(gameObject);
                }
            }
            return;
        }

        if (other.CompareTag("Bubble") && other.TryGetComponent<BasicBubble>(out BasicBubble serverBubble))
        {
            if (serverBubble.OwnerID.Value != OwnerID.Value)
            {
                serverBubble.BubbleCollision(gameObject);
            }
        }

        BubbleCollision(other);
    }

    protected override void Reflect(Vector3 normal)
    {
        if (isReflected) return;
        isReflected = true;
        speed *= -1f;

        if (rangeCoroutine != null)
            StopCoroutine(rangeCoroutine);

        rangeCoroutine = StartCoroutine(BubbleRangeLimit());
    }
}