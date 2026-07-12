using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GiantBubble : BasicBubble
{
    [Header("Big Version")]
    [SerializeField] private float extraOffset = 2f;
    [SerializeField] private int knbDecreaseAngle = 45;
    [SerializeField] private float knbDecreaseIncrement = .25f;
    [SerializeField] private TrailRenderer bigTrail;
    [SerializeField] private Material[] blinkMaterials;
    [SerializeField] private MeshRenderer meshRenderer;
    [Header("Small Version")]
    [SerializeField] private float dmgMini = 3;
    [SerializeField] private float knbMod = .3f;
    [SerializeField] private float sizMod = .25f;
    [SerializeField] private float speedMod = 5f;
    [SerializeField] private GameObject smallHitEffect;
    [SerializeField] private TrailRenderer smallTrail;

    private bool isSmall = false;

    public override void InitialiseBubble(int ID, Vector3 dir, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dir, playerCollider);
        transform.position += direction * extraOffset;
    }

    protected override IEnumerator Inflate()
    {
        if (sphereCollider != null) sphereCollider.excludeLayers += LayerMask.GetMask("Player");
        bool blink = false;
        while (currentSize < size)
        {
            currentSize += inflationSpeed * Time.deltaTime;
            if (currentSize > size) currentSize = size;

            transform.localScale = Vector3.one * currentSize;
            if (!blink && currentSize > size * .9f)
            {
                blink = true;
                StartCoroutine(Blink());
            }
            yield return null;
        }

        InflateOverlapChack();

        if (sphereCollider != null) sphereCollider.excludeLayers -= LayerMask.GetMask("Player");
        hasInflated = true;
    }

    protected override void BubbleMovement()
    {
        // --- PREDICTION FILTER ---
        // Allow movement processing for both the Server and client-side prediction fakes
        if (!IsServer && !isLocalFake) return;
        if (!hasInflated) return;

        base.BubbleMovement();
    }

    private IEnumerator Blink()
    {
        if (GetComponent<Animation>() != null) GetComponent<Animation>().Play();
        if (meshRenderer != null)
        {
            Material[] materials = meshRenderer.materials;
            meshRenderer.materials = blinkMaterials;
            yield return new WaitForSeconds(.15f);
            meshRenderer.materials = materials;
        }
    }

    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped || other == null) return;
        if (!IsServer && !isLocalFake) return;

        // --- BUBBLE-ON-BUBBLE PASS FILTER ---
        if (other.CompareTag("Bubble"))
        {
            // ALWAYS IGNORE YOUR OWN TWIN: Check this first before anything else!
            if (other.TryGetComponent<BasicBubble>(out var otherBubble))
            {
                if (otherBubble.castID == this.castID)
                {
                    return; // Bypass completely so it never falls through to Pop()
                }
            }

            // --- STATE CHANGE: HIT A LEGITIMATE ENEMY BUBBLE ---
            if (!isSmall)
            {
                isSmall = true;
                speed *= speedMod;
                hitEffect = smallHitEffect;
                spellType = SpellType.SmallerGiant;
                size *= sizMod;
                transform.localScale = Vector3.one * size;

                if (bigTrail != null) bigTrail.emitting = false;
                if (smallTrail != null) smallTrail.emitting = true;

                if (IsServer)
                {
                    damage = dmgMini;
                    knockback *= knbMod;
                }
                return; // Transform and keep flying!
            }
        }

        // --- LOCAL FAKE SEPARATION ---
        if (isLocalFake)
        {
            if (other.CompareTag("Player"))
            {
                fizzleEffect = hitEffect;
            }

            Pop();
            return;
        }

        // --- AUTHORITATIVE SERVER HIT CALCULATIONS ---
        if (!isSmall && other.CompareTag("Player"))
        {
            Vector3 v = other.transform.position - transform.position;
            float angle = Vector3.Angle(v, transform.forward);
            if (angle <= 5f)
            {
                knockback *= 1.2f;
            }
            else
            {
                int i = (int)angle / knbDecreaseAngle;
                knockback *= 1 - (knbDecreaseIncrement * i);
            }
        }

        base.BubbleCollision(other);
    }

    protected override void InflateOverlapChack()
    {
        // Use the bubble's configuration size to check for birth-overlaps
        Collider[] overlaps = Physics.OverlapSphere(transform.position, size, LayerMask.GetMask("Player"));
        foreach (Collider col in overlaps)
        {
            // FIX: Explicitly ignore our shooter so the fake bubble doesn't detonate on launch!
            if (ignoredColliders.Contains(col)) continue;

            BubbleCollision(col.gameObject);
            break;
        }
    }
}