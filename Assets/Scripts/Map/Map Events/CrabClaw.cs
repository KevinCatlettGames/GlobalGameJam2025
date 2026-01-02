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
    [SerializeField] private bool isMapEventEnabled = true;
    [SerializeField] private CrabHuntingGrounds huntingGrounds;
    [SerializeField] private LurkingShadow shadow;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform clawTransform;
    [SerializeField] private ParticleSystem snapVFX;
    [Header("Logic")]
    [SerializeField] private float huntingTime = 5f;
    [SerializeField] private float minRange = 5f;
    [Header("Stats")]
    [SerializeField] private float damage = 35f;
    [SerializeField] private float knockback = 10f;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float radius = 5f;
    [SerializeField] private float yLaunchStrength;
    public Vector3 Target;
    public CrabClawStatus Status = CrabClawStatus.inactive;

    private void Start()
    {
        animator.Play("Snap", 0, 1);
    }
    public void StartHunting()
    {
        StartCoroutine(HuntingCoroutine());

    }
    public void StopHunting()
    {
        shadow.LerpShadow(0, .2f);
        Status = CrabClawStatus.inactive;
        StopAllCoroutines();
        CancelInvoke();
    }
    private IEnumerator HuntingCoroutine()
    {
            Status = CrabClawStatus.hunting;
            ResetClaw();
            float timer = huntingTime;
            Vector3 moveVector = Vector3.zero;
            shadow.LerpShadow(1, huntingTime);
            while (timer > 0)
            {
                Target = huntingGrounds.GetClosestTargetPosition(transform.position);
                if (Target != Vector3.zero)
                {
                    if (Target.magnitude < minRange)
                    {
                        Target = Target.normalized * minRange;
                    }
                    moveVector = (Target - transform.position);
                    moveVector = Vector3.ClampMagnitude(moveVector, speed);
                    moveVector *= speed * Time.deltaTime;
                }
                else
                {
                    moveVector = Vector3.zero;
                }
                transform.position = transform.position + moveVector;
                timer -= Time.deltaTime;
                clawTransform.LookAt(Vector3.zero);
                yield return null;
            }
            //Change to anim event
            Snap();
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

    }
}
