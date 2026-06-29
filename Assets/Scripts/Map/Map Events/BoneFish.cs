using System;
using System.Collections;
using FMODUnity;
using UnityEngine;


public class BoneFish : MonoBehaviour
{
    private Animator animator;
    [SerializeField] private ParticleSystem hitVFX;
    [SerializeField] private EventReference hitEvent;
    [SerializeField] private float damage = 8f;
    [SerializeField] private Material swapMaterial;
    [SerializeField] private SkinnedMeshRenderer meshRenderer;
    [SerializeField] private float swapDuration = .15f;
    private bool isSwapped = false;
    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bubble"))
        {
            PlayEffects();
        }
    }
    public float BoneHit()
    {
        PlayEffects();
        return damage;
    }

    private void PlayEffects()
    {
        animator?.SetTrigger("Hit");
        if(!isSwapped)
            StartCoroutine(MaterialSwap());
        RuntimeManager.PlayOneShotAttached(hitEvent, gameObject);
        if (hitVFX)
            hitVFX.Play();
    }

    private IEnumerator MaterialSwap()
    {
        isSwapped = true;
        Material baseMaterial = meshRenderer.material;
        meshRenderer.material = swapMaterial;
        yield return new WaitForSeconds(swapDuration);
        meshRenderer.material = baseMaterial;
        isSwapped = false;
    }
}
