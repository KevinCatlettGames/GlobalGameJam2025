using System.Collections;
using UnityEngine;

public class RisingWall : MonoBehaviour
{
    private Animator animator;
    [SerializeField] private float riseDelay = 2.0f;
    [SerializeField] private float randomDelay = .3f;
    [SerializeField] private GameObject abschieber;
    [SerializeField] private ParticleSystem bubblingParticle;
    [SerializeField] private ParticleSystem riseParticle;
    [SerializeField] private ParticleSystem idleParticle;
    [SerializeField] private ParticleSystem sinkParticle;
    void Start()
    {
        animator = GetComponent<Animator>();

        WallManager.Instance?.AddWall(this);
        animator.Play("Sink", 0, 1);
        //gameObject.SetActive(false);
    }

    public void Rise()
    {
        //gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(RiseCoroutine());
    }
    public void Sink()
    {
        StopAllCoroutines();
        StartCoroutine(SinkCoroutine());
    }
    private IEnumerator RiseCoroutine() 
    {
        bubblingParticle?.Play();
        yield return new WaitForSeconds(riseDelay);
        float r = Random.Range(0, randomDelay);
        yield return new WaitForSeconds(r);
        animator.Play("Rise", 0, 0);
        riseParticle?.Play();
        idleParticle?.Play();
        abschieber.SetActive(true);
        bubblingParticle?.Stop();
    }
    private IEnumerator SinkCoroutine()
    {
        float r = Random.Range(0, randomDelay);
        yield return new WaitForSeconds(r);
        animator.SetTrigger("Sink");
        idleParticle?.Stop();
        sinkParticle?.Play();
        abschieber.SetActive(false);
        yield return null;
        //AnimatorStateInfo animatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
        //float animationLengh = animatorStateInfo.length;
        //yield return new WaitForSeconds(animationLengh);
        //gameObject.SetActive(false);
    }
}
