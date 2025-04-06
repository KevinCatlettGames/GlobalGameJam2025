using UnityEngine;

public class RisingWall : MonoBehaviour
{
    private Animator animator;
    private bool isRising = false;
    [SerializeField] private float damage = 0f;
    [SerializeField] private float knockback = 10f;
    void Start()
    {
        animator = GetComponent<Animator>();
    }

    public void Rise()
    {
        gameObject.SetActive(true);
        isRising = true;
        animator.Play("Rise",0 ,0);
    }

    public void FinishRising()
    {
        isRising = false;
    }
    public void Sink()
    {
        animator.Play("Sink", 0, 0);
    }

    public void OnCollisionEnter(Collision collision)
    {
        if (isRising && collision.gameObject.tag == "Player")
        {
            Vector3 direction = collision.transform.position - transform.position;
            direction.y = 0;
            direction.Normalize();
            PlayerController player = collision.gameObject.GetComponent<PlayerController>();
            player.ApplyKnockback(-1, direction, knockback, damage);
        }
    }
}
