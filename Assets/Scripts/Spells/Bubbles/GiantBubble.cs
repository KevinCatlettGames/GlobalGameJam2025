using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class GiantBubble : BasicBubble
{
    [Header("Particle Systems")]
    [SerializeField] private ParticleSystem stationaryParticleSystem;
    [SerializeField] private ParticleSystem bigParticleSystem;
    [SerializeField] private ParticleSystem smallParticleSystem;
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

    public override void InitialiseBubble(int ID, Vector3 dir, Collider playerCollider, int assignedSpellID, bool fakeWithServerCaster)
    {
        direction = dir.normalized;
        transform.position += direction * extraOffset;

        base.InitialiseBubble(ID, dir, playerCollider, assignedSpellID, fakeWithServerCaster);
    }

    protected override IEnumerator Inflate()
    {
        if (sphereCollider != null)
        {
            sphereCollider.excludeLayers += LayerMask.GetMask("Player");
        }

        bool blink = false;
        while (currentSize < size)
        {
            currentSize += inflationSpeed * Time.deltaTime;
            if (currentSize > size) currentSize = size;

            transform.localScale = Vector3.one * currentSize;
            if (!blink && currentSize > size * .9f)
            {
                blink = true;
                if (IsServer || isLocalFake)
                {
                    StartCoroutine(Blink());
                }
                if (IsServer)
                {
                    StartBlinkClientRpc();
                }
            }
            yield return null;
        }

        if (IsServer || isLocalFake)
        {
            InflateOverlapChack();
        }

        if (sphereCollider != null)
        {
            sphereCollider.excludeLayers -= LayerMask.GetMask("Player");
        }

        if (stationaryParticleSystem != null) stationaryParticleSystem.Stop();
        if (bigParticleSystem != null) bigParticleSystem.Play();
        hasInflated = true;

        if (IsServer)
            SetVFXAfterInflationClientRpc();
    }

    protected override void BubbleMovement()
    {
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

    public override void HandleTrigger(Collider other)
    {
        if (!isLocalFake || !hasInflated || hasPopped) return;

        // If touching another bubble when not yet small, shrink locally
        if (!isSmall && other.CompareTag("Bubble"))
        {
            ApplyShrinkState();
            return;
        }

        base.HandleTrigger(other);
    }

    public override void BubbleCollision(GameObject other)
    {
        if (isLocalFake || hasPopped) return;

        // On Server or networked instance: handles collision with bubbles (shrinking) or players (angle-based knockback)
        if (!isSmall && other != null && other.CompareTag("Bubble"))
        {
            ApplyShrinkState();

            if (IsServer)
                SetCollisionStateClientRpc();

            return;
        }

        if (!isSmall && other != null && other.CompareTag("Player"))
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

    private void ApplyShrinkState()
    {
        if (isSmall) return;

        isSmall = true;
        damage = dmgMini;
        knockback *= knbMod;
        speed *= speedMod;
        hitEffect = smallHitEffect;
        spellType = SpellType.SmallerGiant;

        if (bigTrail != null) bigTrail.emitting = false;
        if (smallTrail != null) smallTrail.emitting = true;

        size *= sizMod;
        currentSize = size;
        transform.localScale = Vector3.one * size;

        if (bigParticleSystem != null) bigParticleSystem.Stop();
        if (smallParticleSystem != null) smallParticleSystem.Play();
    }

    protected override bool DetectsImpact(out Vector3 impactPoint)
    {
        // Don't predict collisions during the inflation phase
        if (!hasInflated)
        {
            impactPoint = transform.position;
            return false;
        }

        return base.DetectsImpact(out impactPoint);
    }

    [ClientRpc]
    private void SetVFXAfterInflationClientRpc()
    {
        if (IsServer || isLocalFake) return;
        if (stationaryParticleSystem != null) stationaryParticleSystem.Stop();
        if (bigParticleSystem != null) bigParticleSystem.Play();
    }

    [ClientRpc]
    private void StartBlinkClientRpc()
    {
        if (IsServer || isLocalFake) return;
        StartCoroutine(Blink());
    }

    [ClientRpc]
    private void SetCollisionStateClientRpc()
    {
        if (IsServer || isLocalFake) return;
        ApplyShrinkState();
    }
}