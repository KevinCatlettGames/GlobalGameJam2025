using UnityEngine;

public class PlayerStateHandler : MonoBehaviour
{
    public Vector3 spawnPosition;
    // Start is called before the first frame update
    void Start()
    {
        spawnPosition = PlayerManager.Instance.AddPlayer(gameObject);
        transform.position = spawnPosition;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Deathzone"))
        {
            PlayerManager.Instance.ReducePlayers();
            CharacterController controller = GetComponent<CharacterController>();
            gameObject.GetComponent<MeshRenderer>().enabled = false;
            controller.enabled = false;
        }
    }

    public void Reset()
    {
        GetComponent<MeshRenderer>().enabled = true;
        CharacterController controller = GetComponent<CharacterController>();
        controller.enabled = false;
        transform.position = spawnPosition;
        controller.enabled = true;
    }
}