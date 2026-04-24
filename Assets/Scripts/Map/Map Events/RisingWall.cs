using FMODUnity;
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
    [SerializeField] private EventReference riseEvent;
    [SerializeField] private EventReference sinkEvent;
    [SerializeField] private bool isField = false;
    private bool isActive = false;
    public bool IsActive { get { return isActive; } }
    void Start()
    {
        animator = GetComponent<Animator>();

        //animator.Play("Sink", 0, 1);
        gameObject.SetActive(false);
    }

    public virtual void Rise()
    {
        if (isActive) return;
        isActive = true;
        StopAllCoroutines();
        StartCoroutine(RiseCoroutine());
    }
    public virtual void Sink(bool instant)
    {
        if (!isActive) return;
        isActive = false; 
        StopAllCoroutines();
        StartCoroutine(SinkCoroutine(instant));
    }
    private IEnumerator RiseCoroutine() 
    {
        bubblingParticle?.Play();
        yield return new WaitForSeconds(riseDelay);
        float r = Random.Range(0, randomDelay);
        yield return new WaitForSeconds(r);
        animator.SetTrigger("Rise");
        RuntimeManager.PlayOneShotAttached(riseEvent, gameObject);
        riseParticle?.Play();
        idleParticle?.Play();
        if (abschieber != null)
            abschieber.SetActive(true);
        if(!isField)
            bubblingParticle?.Stop();
    }
    private IEnumerator SinkCoroutine(bool instant)
    {
        if (!instant)
        {
            float r = Random.Range(0, randomDelay);
            yield return new WaitForSeconds(r);
        }
        else
        {
            animator.speed = 3;
        }
        if(isField)
            bubblingParticle?.Stop();
        animator.SetTrigger("Sink");
        RuntimeManager.PlayOneShotAttached(sinkEvent, gameObject);
        idleParticle?.Stop();
        sinkParticle?.Play();
        //yield return null;
        //AnimatorStateInfo animatorStateInfo = animator.GetCurrentAnimatorStateInfo(0);
        //float animationLengh = animatorStateInfo.length;
        if (abschieber != null)
            abschieber.SetActive(false);
        yield return new WaitForSeconds(2f);
        animator.speed = 1;
        gameObject.SetActive(false);
    }
}
