using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Puddle : NetworkBehaviour
{
    [SerializeField] private float waitDuration;
    [SerializeField] private bool destroyOnRestart = true;
    [SerializeField] private float fadeDuration = .3f;
    private Animator animator;

    private void Start()
    {
        if (destroyOnRestart) GameManager.Instance.OnGameStarted += DestroyOnRestart;

        if (IsServer) StartCoroutine(DespawnAfterDelay(waitDuration));
        animator = GetComponent<Animator>();
    }

    private void DestroyOnRestart()
    {
        if (!IsServer) return;
        NetworkObject.Despawn();  
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        if (destroyOnRestart) GameManager.Instance.OnGameStarted -= DestroyOnRestart;
    }

    private IEnumerator DespawnAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay - fadeDuration);
        animator.SetTrigger("Fade");
        yield return new WaitForSeconds(fadeDuration);
        NetworkObject.Despawn();
        Destroy(gameObject);
    }
}
