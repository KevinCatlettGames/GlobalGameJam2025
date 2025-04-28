using System;
using UnityEngine;
using FMODUnity;
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
    [SerializeField] private GameObject meshObject;
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
        if (!isDead && other.CompareTag("Deathzone"))
        {
            isDead = true;
            GameManager.Instance.ChangePlayerState(playerController.PlayerID, PlayerState.dead);
            playerController.Die();
            Invoke(nameof(DisablePlayer), 2f);
        }
    }

    void DisablePlayer()
    {
        meshObject.SetActive(false);
        controller.enabled = false;
        GameManager.Instance.ChangePlayerState(playerController.PlayerID, PlayerState.disabled);
    }

    public void ResetPlayer()
    {
        CancelInvoke();
        controller.enabled = false;
        meshObject.SetActive(true);
        isDead = false;
        PlayerManager.Instance.ResetPlayerPosition(playerController.PlayerID);
        playerController.ResetPlayerController();
        RuntimeManager.PlayOneShotAttached(startEvent, gameObject);
        controller.enabled = true;
        GameManager.Instance.ChangePlayerState(playerController.PlayerID, PlayerState.alive);
    }
}