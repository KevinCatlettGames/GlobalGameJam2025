using UnityEngine;
using FMODUnity;

public enum PlayerState
{
    alive,
    dead,
    disabled,
    missing
}

public class PlayerStateHandler : MonoBehaviour
{
    [SerializeField] private EventReference deathEvent;
    [SerializeField] private EventReference startEvent;

    private bool isDead = false;
    private PlayerController playerController;
    private CharacterController characterController;

    private void Start()
    {
        RuntimeManager.PlayOneShotAttached(startEvent, gameObject);
        playerController = GetComponent<PlayerController>();
        characterController = GetComponent<CharacterController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!playerController.initialized) return;

        if (other.CompareTag("Deathzone") && !isDead)
        {
            isDead = true;

            if (GameManager.Instance.PlayingLocal)
            {
                GameManager.Instance.ChangePlayerStateLocal(playerController.PlayerID, PlayerState.dead);
            }
            else
            {
                if (!playerController.IsOwner) return;

                GameManager.Instance.ChangePlayerStateServerRpc(playerController.PlayerID, PlayerState.dead);
            }

            playerController.Die();
            Invoke(nameof(DisablePlayer), 2f);
        }
    }

    private void DisablePlayer()
    {
        characterController.enabled = false;
        isDead = false;
        GameManager.Instance.ChangePlayerStateServerRpc(playerController.PlayerID, PlayerState.disabled);
    }

    public void ResetPlayer()
    {
        CancelInvoke();
        characterController.enabled = false;
        PlayerManager.Instance.ResetPlayerPosition(playerController.PlayerID);
        RuntimeManager.PlayOneShotAttached(startEvent, gameObject);
        characterController.enabled = true;
        isDead = false;
        GameManager.Instance.ChangePlayerStateServerRpc(playerController.PlayerID, PlayerState.alive);
    }
}