using Unity.Netcode;
using UnityEngine;
using FMODUnity;

public class SlipBubble : BasicBubble
{
    [SerializeField] private GameObject slimeTrailObject;
    [SerializeField] private GameObject slimePuddleObject;
    [SerializeField] private LayerMask groundedLayerMask;

    private SlimeTrail slimeTrail;

    public override void InitialiseBubble(int ID, float dmg, float knb, float spd, float rng, float siz, float inf, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        base.InitialiseBubble(ID, dmg, knb, spd, rng, siz, inf, dir, soundEvent, playerCollider);

        if (IsServer)
        {
            Vector3 trailPos = new Vector3(transform.position.x, 0.06f, transform.position.z);
            GameObject trail = Instantiate(slimeTrailObject, trailPos, Quaternion.LookRotation(transform.forward));
            slimeTrail = trail.GetComponent<SlimeTrail>();
            slimeTrail.InitialiseTrail(speed);

            trail.GetComponent<NetworkObject>().Spawn();
        }
    }

    protected override void Pop()
    {
        slimeTrail?.StopTrail();

        if (slimeTrail != null)
        {
            NetworkObject trailNetObj = slimeTrail.gameObject.GetComponent<NetworkObject>();
            if (trailNetObj != null && trailNetObj.IsSpawned)
            {
                trailNetObj.Despawn(true);
            }
        }

        base.Pop();
    }

    private void Update()
    {
        if (slimeTrail != null && !Physics.Raycast(transform.position, Vector3.down, 5f, groundedLayerMask))
        {
            if (!slimeTrail.isStopped)
            {
                slimeTrail.StopTrail();
            }
        }
    }

    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped || !IsServer) return;

        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();

            if (GameManager.Instance.playingLocal)
            {
                player.ApplyKnockbackLocal(OwnerID, direction, knockback, damage);
                CreateSlimePuddleLocal(transform.position);
            }
            else
            {
                player.ApplyKnockbackServerRpc(OwnerID, direction, knockback, damage);
                CreateSlimePuddleServerRpc(transform.position);
            }

            Pop();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CreateSlimePuddleServerRpc(Vector3 position)
    {
        Vector3 puddlePos = new Vector3(position.x, 0.06f, position.z);
        GameObject puddle = Instantiate(slimePuddleObject, puddlePos, Quaternion.LookRotation(transform.forward));
        SlimeTrail puddleTrail = puddle.GetComponent<SlimeTrail>();
        puddleTrail.StopTrail();

        puddle.GetComponent<NetworkObject>().Spawn();
    }

    private void CreateSlimePuddleLocal(Vector3 position)
    {
        Vector3 puddlePos = new Vector3(position.x, 0.06f, position.z);
        GameObject puddle = Instantiate(slimePuddleObject, puddlePos, Quaternion.LookRotation(transform.forward));
        SlimeTrail puddleTrail = puddle.GetComponent<SlimeTrail>();
        puddleTrail.StopTrail();
    }

    public override void SetSlippy()
    {
        
    }
}
