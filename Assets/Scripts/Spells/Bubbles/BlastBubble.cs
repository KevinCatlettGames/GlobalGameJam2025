using FMODUnity;
using Unity.Netcode;
using Unity.Services.Multiplayer;
using UnityEngine;

public class BlastBubble : BasicBubble
{
    [SerializeField] private GameObject splat;
    [SerializeField] private LayerMask groundedLayerMask;
    [SerializeField] private float extraOffset = 4.5f;
    [SerializeField] private float shooterKnb = 8f;
    private const float raycastDistance = 5f;

    public override void InitialiseBubble(int ID, float dmg, float knb, float spd, float rng, float siz, float inf, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dmg, knb, spd, rng, siz, inf, dir, soundEvent, playerCollider);
        transform.position += direction * extraOffset;

        if (GameManager.Instance.PlayingLocal)
            playerCollider.GetComponent<PlayerController>().ApplyImpulseLocal(direction * -1, shooterKnb);
        else
            playerCollider.GetComponent<PlayerController>().ApplyImpulseLocal(direction * -1, shooterKnb);
    }
    protected override void InflateOverlapChack()
    {
        base.InflateOverlapChack();
        Pop();
    }
    protected override void Pop()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo, raycastDistance, groundedLayerMask))
        {
            if (IsServer)
            {
                GameObject puddle;
                puddle = Instantiate(splat, hitInfo.point, transform.rotation);
                puddle.GetComponent<NetworkObject>()?.Spawn();
            }
        }
        base.Pop();
    }
}
