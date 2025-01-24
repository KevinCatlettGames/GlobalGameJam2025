using UnityEngine;

public class PlayerPositionInitializer : MonoBehaviour
{
    private Vector3 spawnPosition;
    // Start is called before the first frame update
    void Start()
    {
        spawnPosition = PlayerManager.Instance.GetNonUsedStartPosition();
        transform.position = spawnPosition;
    }
}