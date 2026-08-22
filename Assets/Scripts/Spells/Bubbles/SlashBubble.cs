using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlashBubble : BasicBubble
{
    [Header("SpecialStats")]
    [SerializeField] private GameObject slasherL;
    [SerializeField] private GameObject slasherR;
    [SerializeField] private Transform spinner;
    [SerializeField] private float secondHitDamage;
    [SerializeField] private float secondHitKnockback;

    private List<PlayerController> hitPlayers = new List<PlayerController>();

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

        if(IsServer)
            Pop();
    }

    public void SlasherHit(Vector3 slasherDir, GameObject other)
    {
        if (other == null) return;
        if (!IsServer && !isLocalFake) return;

        direction = isReflected ? -slasherDir : slasherDir;

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
                if (clientBubble.OwnerID != OwnerID)
                {
                    clientBubble.BubbleCollision(gameObject);
                }
            }
            return;
        }

        if (other.CompareTag("Bubble") && other.TryGetComponent<BasicBubble>(out BasicBubble serverBubble))
        {
            if (serverBubble.OwnerID != OwnerID)
            {
                serverBubble.BubbleCollision(gameObject);
            }
        }

        BubbleCollision(other);

        if (other.CompareTag("Player"))
        {
            PlayerController p = other.GetComponent<PlayerController>();
            if (!hitPlayers.Contains(p))
            {
                hitPlayers.Add(p);
            }
        }
    }

    public override void BubbleCollision(GameObject other)
    {
        float originalDamage = damage;
        float originalKnockback = knockback;
        bool isSecondHit = false;
        if (other.CompareTag("Player") && hitPlayers.Contains(other.GetComponent<PlayerController>()))
        {
            damage = secondHitDamage;
            knockback = secondHitKnockback;
            isSecondHit = true;
            isCrit = true;
        }
        base.BubbleCollision(other);
        if (isSecondHit)
        {
            damage = originalDamage;
            knockback = originalKnockback;
            isCrit = false;
        }
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