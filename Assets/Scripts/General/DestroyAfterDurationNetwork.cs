using Unity.Netcode;
using UnityEngine;
using System.Collections;

public class DestroyAfterDurationNetwork : NetworkBehaviour
{
    [SerializeField] private float waitDuration;
    [SerializeField] private bool destroyOnRestart = true;

    private void Start()
    {
        if (destroyOnRestart) GameManager.Instance.OnGameStarted += DestroyOnRestart;

        if (IsServer) StartCoroutine(DespawnAfterDelay(waitDuration));
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
        yield return new WaitForSeconds(delay);
        NetworkObject.Despawn();
        Destroy(gameObject);
    }
}
