using UnityEngine;

public class PlayerStateHandler : MonoBehaviour
{
    public Vector3 spawnPosition;
    // Start is called before the first frame update
    public GameObject meshObject;
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Deathzone"))
        {
            CharacterController controller = GetComponent<CharacterController>();
            controller.Move(new Vector3(0,10,0) * 25);
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
        transform.position = spawnPosition;
        controller.enabled = true;
    }
}