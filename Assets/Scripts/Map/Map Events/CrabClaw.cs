using System.Collections;
using Unity.Netcode;
using UnityEngine;

public enum CrabClawStatus
{
    hunting,
    resting,
    inactive
}
public class CrabClaw : MonoBehaviour
{
    [SerializeField] private LurkingShadow shadow;
    [SerializeField] private Animator animator;
    [SerializeField] private ParticleSystem snapVFX;
    [Header("Stats")]
    [SerializeField] private float damage = 35f;
    [SerializeField] private float knockback = 10f;
    [SerializeField] private float radius = 5f;
    [SerializeField] private float yLaunchStrength = 1f;
    public Vector3 Target;
    public CrabClawStatus Status = CrabClawStatus.inactive;

    private void Start()
    {
        ResetClaw();
    }
    public void Snap()
    {
        animator.Play("Snap", 0, 0);
        snapVFX.Play();
        shadow.LerpShadow(0, .4f);
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
            direction.y = yLaunchStrength;
            if (col.CompareTag("Player"))
            {
                PlayerController player = col.GetComponent<PlayerController>();
                if (player != null)
                {
                    if (GameManager.Instance.PlayingLocal)
                        //ID = -3 to enable knockback with y component
                        player.ApplyKnockbackLocal(-3, direction, knockback, damage);
                    else
                        player.ApplyKnockbackServerRpc(-3, direction, knockback, damage);
                }
            }          
        }
        Status = CrabClawStatus.resting;
    }
    private void ResetClaw()
    {
        animator.Play("Snap", 0, 1);
    }
}
