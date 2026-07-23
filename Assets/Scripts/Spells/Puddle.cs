using System.Collections;
using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        playerID.OnValueChanged += OnPlayerIdAssigned;
        CheckAndHideVisibility(playerID.Value);
    }

    public override void OnNetworkDespawn()
    {
        playerID.OnValueChanged -= OnPlayerIdAssigned;
    }

    public void InitialisePuddle(Collider playerCollider)
    {
        if (!IsServer) return;

        playerID.Value = playerCollider.GetComponent<PlayerController>().PlayerID;
    }

    private void OnPlayerIdAssigned(int previousValue, int newValue)
    {
        CheckAndHideVisibility(newValue);
    }

    private void CheckAndHideVisibility(int currentCasterId)
    {
        if (IsServer || isLocalFake) return;

        // Guard clause for array indexing safely:
        // Make sure the ID isn't negative, fits in the array, and the element isn't null
        if (currentCasterId < 0 || currentCasterId >= GameManager.Instance.Players.Length) return;
        if (GameManager.Instance.Players[currentCasterId] == null) return;

        // Check if the player in that array index is the local owner
        if (GameManager.Instance.Players[currentCasterId].IsOwner)
        {
            Debug.Log("Disabling visibility for the casting client to prevent duplicate visuals.");
            if (spriteRenderer)
                spriteRenderer.enabled = false;
            if (particleSystem)
                particleSystem.gameObject.SetActive(false);
        }
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