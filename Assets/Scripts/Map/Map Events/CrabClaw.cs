using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class CrabClaw : MonoBehaviour
{
    [SerializeField] private bool isMapEventEnabled = true;
    [SerializeField] private CrabHuntingGrounds huntingGrounds;
    [SerializeField] private LurkingShadow shadow;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform clawTransform;
    [SerializeField] private ParticleSystem snapVFX;
    [Header("Logic")]
    [SerializeField] private float startDelay = 8f;
    [SerializeField] private float restetTime = 5f;
    [SerializeField] private float huntingTime = 5f;
    [SerializeField] private Vector3[] resetPoints;
    [Header("Stats")]
    [SerializeField] private float damage = 35f;
    [SerializeField] private float knockback = 10f;
    [SerializeField] private float speed = 5f;
    [SerializeField] private float radius = 5f;
    [SerializeField] private float yLaunchStrength;
    private bool isHunting = false;
    

    private void Start()
    {
        if (isMapEventEnabled)
            Invoke(nameof(StartHunting),7);
        else
            Destroy(gameObject);

        if (TransportSwitcher.Instance)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            GameManager.Instance.OnGameStarted += StartHunting;
            GameManager.Instance.OnGameEnded += StopHunting;
        }
        else
        {
            GameManager.Instance.OnGameStarted += StartHunting;
            GameManager.Instance.OnGameEnded += StopHunting;
        }
        animator.Play("Snap", 0, 1);
    }
    private void StartHunting()
    {
        isHunting = true;
        StartCoroutine(HuntingCoroutine(startDelay));

    }
    private void StopHunting()
    {
        shadow.LerpShadow(0, .2f);
        isHunting = false;
        StopAllCoroutines();
        CancelInvoke();
    }
    private IEnumerator HuntingCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        while (isHunting)
        {
            ResetClaw();
            float timer = huntingTime;
            Vector3 target;
            Vector3 moveVector = Vector3.zero;
            shadow.LerpShadow(1, huntingTime);
            while (timer > 0)
            {
                target = huntingGrounds.GetClosestTargetPosition(transform.position);
                if (target != Vector3.zero)
                {
                    moveVector = (target - transform.position);
                    moveVector = Vector3.ClampMagnitude(moveVector, speed);
                    moveVector *= speed * Time.deltaTime;
                }
                else
                {
                    moveVector = Vector3.zero;
                }
                transform.position = transform.position + moveVector;
                timer -= Time.deltaTime;
                yield return null;
            }
            //Change to anim event
            clawTransform.LookAt(Vector3.zero);
            Snap();
            yield return new WaitForSeconds(restetTime);
        }

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
    }
    private void ResetClaw()
    {
        int r = Random.Range(0, resetPoints.Length);
        transform.position = resetPoints[r];
    }
    private void OnDestroy()
    {
        if (TransportSwitcher.Instance)
        {
            if (!NetworkManager.Singleton.IsServer) return;
            GameManager.Instance.OnGameStarted -= StartHunting;
            GameManager.Instance.OnGameEnded -= StopHunting;
        }
        else
        {
            GameManager.Instance.OnGameStarted -= StartHunting;
            GameManager.Instance.OnGameEnded -= StopHunting;
        }
    }
}
