using FMODUnity;
using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class Clam : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private float damage = 56;
    [SerializeField] private float knockback = .5f;
    [SerializeField] private float activeDelay = .1f;
    [SerializeField] private float stunDuration = .2f;

    [Header("VFX / SFX")]
    [SerializeField] private ParticleSystem riseParticleSystem;
    [SerializeField] private ParticleSystem snapParticleSystem;
    [SerializeField] private ParticleSystem jumpParticleSystem;
    [SerializeField] protected EventReference crunchSoundEvent;

    private bool isActive = false;
    public bool IsAvailable = true;

    [Header("Rendering")]
    [SerializeField] private Material[] materials;
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

    public void Rise(int spellID)
    {
        StopAllCoroutines();
        StartCoroutine(RiseCoroutine(spellID));
    }

    private IEnumerator RiseCoroutine(int spellID)
    {
        IsAvailable = false;
        int r = Random.Range(0, materials.Length);
        meshRenderer.material = materials[r];

        clamItem.gameObject.SetActive(true);
        clamItem.SetupSpellClientRpc(spellID);

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
            animator.SetTrigger("Snap");
            pearlAnimator.SetTrigger("Snap");
            jumpParticleSystem?.Play();
        }
    }

    public void Snap()
    {
        RuntimeManager.PlayOneShotAttached(crunchSoundEvent, gameObject);
        Collider[] snapOverlaps = Physics.OverlapSphere(transform.position, radius, LayerMask.GetMask("Player"));
        Vector3 direction;

        if (snapOverlaps != null && snapOverlaps.Length > 0)
        {
            snapParticleSystem?.Play();
            foreach (Collider col in snapOverlaps)
            {
                if (col == null) continue;
                PlayerController player = col.GetComponent<PlayerController>();
                if (player == null) continue;

                direction = player.GetComponent<CharacterController>().velocity;

                if (NetworkManager.Singleton.IsServer)
                {
                    if (GameManager.Instance.PlayingLocal)
                        player.ApplyKnockbackLocal(-1, direction, knockback, damage);
                    else
                        player.ApplyKnockbackClientRpc(-1, direction, knockback, damage);

                    player.Stun(stunDuration);
                }
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
            IsAvailable = true;
            gameObject.SetActive(false);
        }
    }
}