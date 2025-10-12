using UnityEngine;

public class RisingWall : MonoBehaviour
{
    private Animator animator;
    [SerializeField] private GameObject abschieber;
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
        abschieber.SetActive(true);
    }
    public void Sink()
    {
        animator.Play("Sink", 0, 0);
        abschieber.SetActive(false);
    }
    public void FinishSinking()
    {
        gameObject.SetActive(false);
    }
}
