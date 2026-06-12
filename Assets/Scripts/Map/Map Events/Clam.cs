using System;
using System.Collections;
using UnityEngine;
using static UnityEngine.UI.Image;

public class Clam : MonoBehaviour
{
    [SerializeField] private float damage = 56;

    private float riseDuration = .5f;
    private bool isActive = false;
    public bool IsActive { get { return isActive; } }
    private Animator animator;
    private float radius = 0;
    public Action OnSnap;

    private void Awake()
    {
        animator = GetComponent<Animator>();
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
        animator.SetTrigger("Rise");
        yield return new WaitForSeconds(riseDuration);
        isActive = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isActive && other.CompareTag("Player"))
        {
            isActive = false;
            //Effects
            //Sound
            animator.SetTrigger("Snap");
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
            animator.SetTrigger("Snap");
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
