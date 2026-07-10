using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
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
        base.InitialiseBubble(ID, dir, playerCollider);
        transform.position += direction * extraOffset;
    }

    protected override IEnumerator Inflate()
    {
        sphereCollider.excludeLayers += LayerMask.GetMask("Player");
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

        sphereCollider.excludeLayers -= LayerMask.GetMask("Player");
        stationaryParticleSystem.Stop();
        bigParticleSystem.Play();
        hasInflated = true;
    }

    protected override void BubbleMovement()
    {
        if (!IsServer) return;
        if (!hasInflated) return;
        base.BubbleMovement();
    }
    private IEnumerator Blink()
    { 
        GetComponent<Animation>().Play();
        Material[] materials = meshRenderer.materials;
        meshRenderer.materials = blinkMaterials;
        yield return new WaitForSeconds(.15f);
        meshRenderer.materials = materials;
    }
    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped) return;
        if (!isSmall && other.CompareTag("Bubble"))
        {
            isSmall = true;
            damage = dmgMini;
            knockback *= knbMod;
            speed *= speedMod;
            hitEffect = smallHitEffect;
            spellType = SpellType.SmallerGiant;
            bigTrail.emitting = false;
            smallTrail.emitting = true;
            size *= sizMod;
            transform.localScale = Vector3.one * size;
            bigParticleSystem.Stop();
            smallParticleSystem.Play();
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
}