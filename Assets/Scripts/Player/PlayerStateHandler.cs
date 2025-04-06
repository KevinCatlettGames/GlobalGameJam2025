using System;
using UnityEngine;
using FMODUnity;
using UnityEngine.UI;
public class PlayerStateHandler : MonoBehaviour
{
    public Vector3 spawnPosition;
    public GameObject meshObject;
    public Image aimIndicator; 
    [SerializeField] private EventReference deathEvent;
    [SerializeField] private EventReference startEvent;
    private bool isDead = false; 
    private PlayerController playerController;
    
    private void Start()
    {
        RuntimeManager.PlayOneShotAttached(startEvent, gameObject);
        playerController = gameObject.GetComponent<PlayerController>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isDead == other.CompareTag("Deathzone"))
        {
            isDead = true;
            RuntimeManager.PlayOneShotAttached(deathEvent, gameObject);
            playerController.Die();
            Invoke(nameof(DisablePlayer), 2f);
        }
    }

    void DisablePlayer()
    {
        PlayerManager.Instance.ReducePlayers();
        meshObject.SetActive(false);
        CharacterController controller = GetComponent<CharacterController>();
        controller.enabled = false;
    }

    public void Reset()
    {
        CharacterController controller = GetComponent<CharacterController>();
        controller.enabled = false;
        meshObject.SetActive(true);
        isDead = false; 
        transform.position = spawnPosition;
        playerController.ResetOnNewGame();
        RuntimeManager.PlayOneShotAttached(startEvent, gameObject);
        controller.enabled = true;
    }
}