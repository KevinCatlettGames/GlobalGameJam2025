using UnityEngine;

public class RisingWall : MonoBehaviour
{
    private Animator animator;
    [SerializeField] private GameObject abschieber;
    [SerializeField] private ParticleSystem riseParticle;
    [SerializeField] private ParticleSystem idleParticle;
    [SerializeField] private ParticleSystem sinkParticle;
    void Start()
    {
        animator = GetComponent<Animator>();

        WallManager.Instance?.AddWall(this);
        gameObject.SetActive(false);
    }

    public void Rise()
    {
        gameObject.SetActive(true);
        animator.Play("Rise",0 ,0);
        riseParticle?.Play();
        idleParticle?.Play();
        abschieber.SetActive(true);
    }
    public void Sink()
    {
        animator.SetTrigger("Sink");
        idleParticle?.Stop();
        sinkParticle?.Play();
        abschieber.SetActive(false);
    }
    public void FinishSinking()
    {
        gameObject.SetActive(false);
    }
}
