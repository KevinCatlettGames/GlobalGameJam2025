using UnityEngine;


public enum PlayerState
{
    alive,
    dead,
    disabled,
    missing,
    pendingRespawn
}

public class PlayerStateHandler : MonoBehaviour
{
    [SerializeField] private float respawnTime = 3f;
    [SerializeField] private Transform setRespawnPosition;
    private SO_GameSettings gameSettings;

    private bool canDie = false;
    private PlayerController playerController;
    private CharacterController characterController;
    private int currentLifes = 0;
    [SerializeField] private int maxLifes = 0;

    private void Start()
    {
        playerController = GetComponent<PlayerController>();
        characterController = GetComponent<CharacterController>();
        gameSettings = GameManager.Instance.GetGameSettings();
        if (gameSettings != null)
        {
            maxLifes = gameSettings.Lifes;
            if (maxLifes != -1)
            {
                ResetLifes();
                GameManager.Instance.OnGameStarted += ResetLifes;
            }

        }
        TargetGroupManager.Instance?.AddToGroup(transform);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!playerController.initialized) return;

        if (other.CompareTag("Deathzone") && canDie)
        {
            KillPlayer();
        }
    }
    public void KillPlayer()
    {
        canDie = false;

        LooseLife();

        TargetGroupManager.Instance?.RemoveFromGroup(transform);
        playerController.Die();
        Invoke(nameof(DisablePlayer), 2f);
    }

    private void DisablePlayer()
    {
        characterController.enabled = false;
        canDie = false;
        //GameManager.Instance.ChangePlayerStateServerRpc(playerController.PlayerID, PlayerState.disabled);
    }

    public void ResetPlayer()
    {
        canDie = false;
        CancelInvoke();
        characterController.enabled = false;
        if (setRespawnPosition == null)
        {
            PlayerManager.Instance.ResetPlayerPosition(playerController.PlayerID);
        }
        else
        {
            transform.position = setRespawnPosition.position;
            transform.rotation = setRespawnPosition.rotation;
        }
        characterController.enabled = true;
        canDie = true;
        GameManager.Instance.ChangePlayerStateServerRpc(playerController.PlayerID, PlayerState.alive);
        TargetGroupManager.Instance?.AddToGroup(transform);
    }
    public void EnableDeath()
    {
        canDie = true;
    }
    private void LooseLife()
    {
        if (maxLifes != -1)
        {
            currentLifes--;
            if (currentLifes <= 0)
            {

                if (GameManager.Instance.PlayingLocal)
                {
                    GameManager.Instance.ChangePlayerStateLocal(playerController.PlayerID, PlayerState.dead);
                }
                else
                {
                    if (!playerController.IsOwner) return;

                    GameManager.Instance.ChangePlayerStateServerRpc(playerController.PlayerID, PlayerState.dead);
                }
                return;
            }
            else
            {
                if (GameManager.Instance.PlayingLocal)
                {
                    GameManager.Instance.ChangePlayerStateLocal(playerController.PlayerID, PlayerState.pendingRespawn);
                }
                else
                {
                    if (!playerController.IsOwner) return;

                    GameManager.Instance.ChangePlayerStateServerRpc(playerController.PlayerID, PlayerState.pendingRespawn);
                }
            }
        }
        Invoke(nameof(Respawn), respawnTime); 
    }
    private void Respawn()
    {
        playerController.ResetPlayerController(true);
    }
    private void ResetLifes()
    {
        currentLifes = maxLifes;
    }
    private void OnDestroy()
    {
        if (maxLifes != -1)
        {
            GameManager.Instance.OnGameStarted -= ResetLifes;
        }
    }
}