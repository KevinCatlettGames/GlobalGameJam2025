using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class CrabClaw : MonoBehaviour
{
    [SerializeField] private LurkingShadow shadow;
    [SerializeField] private Animator[] animators;
    [SerializeField] private ParticleSystem snapVFX;
    [Header("Stats")]
    [SerializeField] private float damage = 35f;
    [SerializeField] private float knockback = 10f;
    [SerializeField] private float radius = 5f;
    [SerializeField] private float snapDelay = 0.15f;

    private void Start()
    {
        ResetClaw();
    }
    public void StartSnap()
    {
        foreach (Animator anim in animators)
        {
            anim.Play("Snap", 0, 0);
        }
        snapVFX.Play();
        shadow.LerpShadow(0, .4f);
        Invoke(nameof(Snap), snapDelay);
    }
    public void Snap()
    {
        Collider[] snapOverlaps = Physics.OverlapSphere(transform.position, radius, LayerMask.GetMask("Player"));
        Vector3 origin;
        Vector3 direction;
        foreach (Collider col in snapOverlaps)
        {
            if (col == null) continue;
            origin = transform.position;
            direction = col.transform.position - transform.position;
            if (direction.sqrMagnitude < .3f)
                direction = Vector3.forward;
            if (col.CompareTag("Player"))
            {
                PlayerController player = col.GetComponent<PlayerController>();
                if (player != null)
                {
                    if (GameManager.Instance.PlayingLocal)
                        player.ApplyKnockbackLocal(-1, direction, knockback, damage, false);
                    else
                        player.ApplyKnockbackServerRpc(-1, direction, knockback, damage, false);
                }
            }          
        }
    }
    private void ResetClaw()
    {
        foreach (Animator anim in animators)
        {
            anim.Play("Snap", 0, 1);
        }
    }
}
