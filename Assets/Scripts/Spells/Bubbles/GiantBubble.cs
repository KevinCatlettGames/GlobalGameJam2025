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

    public override void InitialiseBubble(int ID, Vector3 dir, Collider playerCollider)
    {
        // FIX 1: Shift the position BEFORE running base setup logic. 
        // This ensures the initial position is perfectly calculated before fakes register or servers serialize.
        direction = dir.normalized;
        transform.position += direction * extraOffset;

        base.InitialiseBubble(ID, dir, playerCollider);
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
                if(IsServer || isLocalFake)
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

        // Only let the server or an independent local singleplayer frame handle instant overlap pops
        if (IsServer || isLocalFake)
        {
            InflateOverlapChack();
        }

        if (sphereCollider != null)
        {
            sphereCollider.excludeLayers -= LayerMask.GetMask("Player");
        }

        stationaryParticleSystem.Stop();
        bigParticleSystem.Play();
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

    public override void BubbleCollision(GameObject other)
    {
        // FIX 2: Local fakes must NEVER process authority collisions, or they destroy themselves prematurely!
        if (isLocalFake) return;
        if (hasPopped) return;

        if (!isSmall && other.CompareTag("Bubble"))
        {
            isSmall = true;
            damage = dmgMini;
            knockback *= knbMod;
            speed *= speedMod;
            hitEffect = smallHitEffect;
            spellType = SpellType.SmallerGiant;
            if (bigTrail != null) bigTrail.emitting = false;
            if (smallTrail != null) smallTrail.emitting = true;
            size *= sizMod;
            transform.localScale = Vector3.one * size;
            bigParticleSystem.Stop();
            smallParticleSystem.Play();

            if (IsServer)
                SetCollisionStateClientRpc();

            return;
        }

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

    [ClientRpc]
    void SetVFXAfterInflationClientRpc()
    {
        if (IsServer) return;
        stationaryParticleSystem.Stop();
        bigParticleSystem.Play();
    }

    [ClientRpc]
    void StartBlinkClientRpc()
    {
        if (IsServer) return;
        StartCoroutine(Blink());
    }

    [ClientRpc]
    void SetCollisionStateClientRpc()
    {
        if (IsServer) return;
        isSmall = true;
        damage = dmgMini;
        knockback *= knbMod;
        speed *= speedMod;
        hitEffect = smallHitEffect;
        spellType = SpellType.SmallerGiant;
        if (bigTrail != null) bigTrail.emitting = false;
        if (smallTrail != null) smallTrail.emitting = true;
        size *= sizMod;
        transform.localScale = Vector3.one * size;
        bigParticleSystem.Stop();
        smallParticleSystem.Play();
    }
}