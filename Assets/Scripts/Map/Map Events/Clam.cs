using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

public class Clam : MonoBehaviour
{
    [SerializeField] private float damage = 56;
    [SerializeField] private float activeDelay = .1f;
    [SerializeField] private ParticleSystem riseParticleSystem;
    [SerializeField] private ParticleSystem snapParticleSystem;
    [SerializeField] private Material[] materials;

    private bool isActive = false;
    public bool IsAvailble = true;
    [SerializeField] private Animator animator;
    [SerializeField] private Animator pearlAnimator;
    [SerializeField] private ClamItem clamItem;
    [SerializeField] private SkinnedMeshRenderer meshRenderer;
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
        IsAvailble = false;
        int r = Random.Range(0, materials.Length);
        meshRenderer.material = materials[r];
        clamItem.gameObject.SetActive(true);
        clamItem.SetupSpellClientRpc(ItemSpawner.Instance.GetRandomLegalSpellID());
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
            IsAvailble = true;
            gameObject.SetActive(false);
        }
    }
}
