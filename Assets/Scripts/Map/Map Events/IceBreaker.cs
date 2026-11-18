using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;

public class IceBreaker : MonoBehaviour
{
    PlayerController playerController;
    private const float raycastDistance = 5f;
    void Start()
    {
        playerController = GetComponent<PlayerController>();
        playerController.OnBeginSprint.AddListener(CrackFloor);
    }
    private void CrackFloor()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo, raycastDistance, LayerMask.GetMask("Ground")))
        {
            IceFloor iceFloor = hitInfo.collider.gameObject.GetComponent<IceFloor>();
            if (iceFloor != null)
            {
                iceFloor.Crack();
            }
        }
    }
    private void OnDestroy()
    {
        playerController.OnBeginSprint.RemoveListener(CrackFloor);
    }
}
