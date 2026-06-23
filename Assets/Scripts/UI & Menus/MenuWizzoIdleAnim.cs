using System.Collections;
using UnityEngine;

public class MenuWizzoIdleAnim : MonoBehaviour
{
    [SerializeField] private float minTime = 5f;
    [SerializeField] private float maxTime = 15f;

    [SerializeField] private bool randomAnimation = true;

    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        StartCoroutine(RandomIdleBreakRoutine());
    }

    private IEnumerator RandomIdleBreakRoutine()
    {
        while (true)
        {
            float timer = Random.Range(minTime, maxTime);

            yield return new WaitForSeconds(timer);

            if (!randomAnimation)
            {
                animator.SetTrigger("IdleBreak1");
                continue;
            }

            int randomAnim = Random.Range(0, 2);

            if (randomAnim == 0)
            {
                animator.SetTrigger("IdleBreak1");
            }
            else
            {
                animator.SetTrigger("IdleBreak2");
            }
        }
    }
}