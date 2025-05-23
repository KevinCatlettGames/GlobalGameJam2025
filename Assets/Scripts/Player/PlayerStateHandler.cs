using UnityEngine;
using FMODUnity;
using Unity.VisualScripting;
using UnityEngine.UI;

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
    private CharacterController controller;
    
    private void Start()
    {
        RuntimeManager.PlayOneShotAttached(startEvent, gameObject);
        playerController = gameObject.GetComponent<PlayerController>();
        controller = gameObject.GetComponent<CharacterController>();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (GameManager.Instance.playingLocal)
        {
            if (!playerController.initialized) return;

            if (!isDead && other.CompareTag("Deathzone"))
            {
                isDead = true;
                GameManager.Instance.ChangePlayerStateLocal(playerController.PlayerID, PlayerState.dead);
                playerController.Die();
                Invoke(nameof(DisablePlayer), 2f);
            }
        }
        else
        {
            if (!playerController.initialized || !playerController.IsOwner) return;

            if (!isDead && other.CompareTag("Deathzone"))
            {
                isDead = true;
                GameManager.Instance.ChangePlayerStateServerRpc(playerController.PlayerID, PlayerState.dead);
                playerController.Die();
                
                Invoke(nameof(DisablePlayer), 2f);
            }
        }
    }

    void DisablePlayer()
    {
        controller.enabled = false;
        isDead = false;
        GameManager.Instance.ChangePlayerStateServerRpc(playerController.PlayerID, PlayerState.disabled);
    }

    public void ResetPlayer()
    {
        CancelInvoke();
        controller.enabled = false;
        PlayerManager.Instance.ResetPlayerPosition(playerController.PlayerID);
        RuntimeManager.PlayOneShotAttached(startEvent, gameObject);
        controller.enabled = true;
        isDead = false;
        GameManager.Instance.ChangePlayerStateServerRpc(playerController.PlayerID, PlayerState.alive);
    }
}