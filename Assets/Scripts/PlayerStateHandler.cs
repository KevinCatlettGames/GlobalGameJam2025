using System;
using UnityEngine;
using FMODUnity;
using UnityEngine.UI;
public class PlayerStateHandler : MonoBehaviour
{
    
    public Vector3 spawnPosition;
    // Start is called before the first frame update
    public GameObject meshObject;
    public Image aimIndicator; 
    [SerializeField] private EventReference deathEvent;
    [SerializeField] private EventReference startEvent;
    private bool endTriggered = false; 
    
    private void Start()
    {
        RuntimeManager.PlayOneShotAttached(startEvent, gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Deathzone") && !endTriggered)
        {
            endTriggered = true; 
            RuntimeManager.PlayOneShotAttached(deathEvent, gameObject);
            Invoke(nameof(DisablePlayer), 5f);
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
        endTriggered = false; 
        transform.position = spawnPosition;
        RuntimeManager.PlayOneShotAttached(startEvent, gameObject);
        controller.enabled = true;
    }
}