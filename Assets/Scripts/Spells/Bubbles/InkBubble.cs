using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;

public class InkBubble : BasicBubble
{
    [SerializeField] private GameObject inkPuddle;
    [SerializeField] private LayerMask groundedLayerMask;
    private const float raycastDistance = 5f;
    private bool spawnInk = true;
    public override void BubbleCollision(GameObject other)
    {
        if (other.CompareTag("Bubble") && popOnBubbleHit) spawnInk = false;
        base.BubbleCollision(other);
    }
    private void SpawnInk()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo, raycastDistance, groundedLayerMask))
        {
            if (IsServer)
            {
                GameObject puddle;
                puddle = Instantiate(inkPuddle, hitInfo.point, transform.rotation);
                puddle.GetComponent<NetworkObject>()?.Spawn();
            }
        }
    }
    protected override void Pop()
    {
        if (spawnInk)
        {
            SpawnInk();
        }
        base.Pop();
    }
}
