using UnityEngine;
using FMODUnity;

public class PlayerStateHandler : MonoBehaviour
{
    public Vector3 spawnPosition;
    // Start is called before the first frame update
    public GameObject meshObject;
<<<<<<< Updated upstream
=======
    public SkinnedMeshRenderer meshRenderer;
    
    [Header("Sound Events")]
    [SerializeField] protected EventReference castEventStruct;
>>>>>>> Stashed changes
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Deathzone"))
        {
            CharacterController controller = GetComponent<CharacterController>();
            controller.Move(new Vector3(0,10,0));
            RuntimeManager.PlayOneShotAttached(castEventStruct, this.gameObject);
            Invoke(nameof(DisablePlayer), .5f);
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
        transform.position = spawnPosition;
        controller.enabled = true;
    }
}