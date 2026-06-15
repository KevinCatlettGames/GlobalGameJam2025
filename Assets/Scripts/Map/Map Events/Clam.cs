using System;
using System.Collections;
using UnityEngine;

public class Clam : MonoBehaviour
{
    [SerializeField] private float damage = 56;
    [SerializeField] private float activeDelay = .1f;
    [SerializeField] private ParticleSystem riseParticleSystem;
    [SerializeField] private ParticleSystem snapParticleSystem;

    private bool isActive = false;
    public bool IsActive { get { return isActive; } }
    [SerializeField] private Animator animator;
    [SerializeField] private Animator pearlAnimator;
    private float radius = 0;
    public Action OnSnap;

    private void Awake()
    {
        radius = GetComponent<SphereCollider>().radius;
    }
    public void Rise()
    {
        StopAllCoroutines();
        StartCoroutine(RiseCoroutine());
    }

    private IEnumerator RiseCoroutine()
    {
        //Effects
        //Sound
        riseParticleSystem?.Play();
        animator.Play("Rise");
        pearlAnimator.Play("Rise");
        yield return new WaitForSeconds(activeDelay);
        isActive = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isActive && other.CompareTag("Player"))
        {
            isActive = false;
            //Effects
            //Sound
            snapParticleSystem?.Play();
            animator.SetTrigger("Snap");
            pearlAnimator.SetTrigger("Snap");
        }
    }

    // Called by animation
    public void Snap()
    {
        //Effects
        //Sound
        Collider[] snapOverlaps = Physics.OverlapSphere(transform.position, radius, LayerMask.GetMask("Player"));
        Vector3 direction;
        foreach (Collider col in snapOverlaps)
        {
            if (col == null) continue;
            direction = col.transform.position - transform.position;

            PlayerController player = col.GetComponent<PlayerController>();
            if (player != null)
            {

                if (GameManager.Instance.PlayingLocal)
                    player.ApplyKnockbackLocal(-1, direction, .1f, damage);
                else
                    player.ApplyKnockbackServerRpc(-1, direction, .1f, damage);
            }         
        }
        OnSnap?.Invoke();
    }

    public void DisableClam()
    {
        if (isActive)
        {
            isActive = false;
            snapParticleSystem?.Play();
            animator.SetTrigger("Snap");
            pearlAnimator.SetTrigger("Snap");
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
