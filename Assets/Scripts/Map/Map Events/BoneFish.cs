using System;
using UnityEngine;


public class BoneFish : MonoBehaviour
{
    private Animator animator;
    private void Start()
    {
        animator = GetComponent<Animator>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player") || collision.gameObject.CompareTag("Bubble"))
        {
            Debug.Log("Hit");
            animator?.SetTrigger("Hit");
        }
    }
}
