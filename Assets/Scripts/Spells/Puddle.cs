using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Puddle : NetworkBehaviour
{
    [SerializeField] private float waitDuration;
    [SerializeField] private bool destroyOnRestart = true;
    [SerializeField] private float fadeDuration = .3f;
    private Animator animator;
    public bool isLocalFake = false;
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] ParticleSystem particleSystem;
    public NetworkVariable<int> playerID = new NetworkVariable<int>();

    private void Start()
    {
        if (destroyOnRestart) GameManager.Instance.OnGameStarted += DestroyOnRestart;

        if (IsServer || isLocalFake) StartCoroutine(DespawnAfterDelay(waitDuration));
        animator = GetComponent<Animator>();
    }

    public void InitialisePuddle(Collider playerCollider)
    {
        if (!IsServer) return;

        playerID.Value = playerCollider.GetComponent<PlayerController>().PlayerID;
    }

    private void DestroyOnRestart()
    {
        if (!IsServer && !isLocalFake) return;

        if (IsServer)
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

        if (IsServer)
            SetFadeAnimClientRpc();
        if (isLocalFake)
            animator.SetTrigger("Fade");

        yield return new WaitForSeconds(fadeDuration);

        if (IsServer)
            NetworkObject.Despawn();

        Destroy(gameObject);
    }

    [ClientRpc]
    void SetFadeAnimClientRpc()
    {
        if (animator)
            animator.SetTrigger("Fade");
    }
}