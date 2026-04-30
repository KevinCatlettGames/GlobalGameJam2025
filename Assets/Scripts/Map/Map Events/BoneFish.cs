using System;
using UnityEngine;


public class BoneFish : MonoBehaviour
{
    private Animator animator;
    [SerializeField] private ParticleSystem hitVFX;
    [SerializeField] private float damage = 8f;
    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Bubble"))
        {
            animator?.SetTrigger("Hit");
            if (hitVFX)
                hitVFX.Play();
        }
    }
    public float BoneHit()
    {
        animator?.SetTrigger("Hit");
        if (hitVFX)
            hitVFX.Play();
        return damage;
    }
}
