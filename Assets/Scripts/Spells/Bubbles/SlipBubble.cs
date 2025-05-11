using System.Collections;
using Unity.Netcode;
using UnityEngine;
using FMODUnity;

public class SlipBubble : BasicBubble
{
    [SerializeField] private GameObject slimeTrailObject;
    [SerializeField] private GameObject slimePuddleObject;
    [SerializeField] private LayerMask groundedLayerMask;
    private SlimeTrail slimeTrail;

    public override void InitialiseBubble(int ID, float dmg, float knb, float spd, float rng, float siz, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dmg, knb, spd, rng, siz, dir, soundEvent, playerCollider);

        if (IsServer) // Only server should instantiate networked objects
        {
            GameObject trail = Instantiate(slimeTrailObject, new Vector3(transform.position.x, 0.06f, transform.position.z), Quaternion.LookRotation(transform.forward));
            slimeTrail = trail.GetComponent<SlimeTrail>();
            slimeTrail.InitialiseTrail(speed);

            // Use NetworkObject to spawn on clients
            trail.GetComponent<NetworkObject>().Spawn();
        }
    }

    protected override void Pop()
    {
        if (IsServer)
        {
            slimeTrail?.StopTrail();

            // Optionally destroy the slime trail and puddle networked objects
            if (slimeTrail != null && slimeTrail.gameObject != null)
            {
                NetworkObject trailNetworkObj = slimeTrail.gameObject.GetComponent<NetworkObject>();
                trailNetworkObj?.Despawn(true);
            }

            // Call the base Pop method to handle other effects
            base.Pop();
        }
    }

    private void Update()
    {
        if (slimeTrail != null && !Physics.Raycast(transform.position, Vector3.down, 5f, groundedLayerMask))
        {
            if (!slimeTrail.isStopped)
            {
                slimeTrail?.StopTrail();
            }
        }
    }

    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped.Value) return;

        if (other.CompareTag("Player"))
        {
            // Server handles the collision logic
            if (IsServer)
            {
                PlayerController player = other.GetComponent<PlayerController>();
                player.ApplyKnockbackServerRpc(OwnerID.Value, direction.Value, knockback, damage);

                // Create slime puddle only on the server, then sync to clients
                CreateSlimePuddleServerRpc(transform.position);
            }

            Pop();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CreateSlimePuddleServerRpc(Vector3 position)
    {
        // Instantiate puddle on the server, and spawn it across clients
        GameObject puddle = Instantiate(slimePuddleObject, new Vector3(position.x, 0.06f, position.z), Quaternion.LookRotation(transform.forward));
        SlimeTrail puddleTrail = puddle.GetComponent<SlimeTrail>();
        puddleTrail.StopTrail();

        // Network spawn puddle object
        puddle.GetComponent<NetworkObject>().Spawn();
    }

    public override void SetSlippy()
    {
        return;
    }
}
