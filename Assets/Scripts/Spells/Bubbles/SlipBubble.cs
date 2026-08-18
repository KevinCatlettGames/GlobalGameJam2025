using Unity.Netcode;
using UnityEngine;

public class SlipBubble : BasicBubble
{
    [SerializeField] private GameObject slimeTrailObject;
    [SerializeField] private GameObject slimePuddleObject;
    [SerializeField] private LayerMask groundedLayerMask;

    private SlimeTrail slimeTrail;

    public override void InitialiseBubble(int ID, Vector3 dir, Collider playerCollider, int assignedSpellID, bool fakeWithServerCaster)
    {
        base.InitialiseBubble(ID, dir, playerCollider, assignedSpellID, fakeWithServerCaster);

        Vector3 trailPos = new Vector3(transform.position.x, 0.06f, transform.position.z);

        if (isLocalFake)
        {
            if (slimeTrailObject != null)
            {
                GameObject trail = Instantiate(slimeTrailObject, trailPos, Quaternion.LookRotation(transform.forward));
                Destroy(trail.GetComponent<NetworkObject>());
                slimeTrail = trail.GetComponent<SlimeTrail>();
                slimeTrail?.InitialiseTrail(speed);
            }
        }
        else if (IsServer)
        {
            GameObject trail = Instantiate(slimeTrailObject, trailPos, Quaternion.LookRotation(transform.forward));
            slimeTrail = trail.GetComponent<SlimeTrail>();
            slimeTrail?.InitialiseTrail(speed);

            trail.GetComponent<NetworkObject>()?.Spawn();
        }
    }

    protected override void Pop()
    {
        slimeTrail?.StopTrail();

        if (slimeTrail != null && !isLocalFake)
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
        if (!IsServer && !isLocalFake) return;

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
        if (hasPopped || other == null) return;
        if (!IsServer && !isLocalFake) return;

        if (isLocalFake)
        {
            if (other.CompareTag("Player"))
            {
                CreateSlimePuddleLocal(transform.position);
                Pop();
            }
            else if (other.CompareTag("Wall"))
            {
                Pop();
            }
            return;
        }

        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                if (GameManager.Instance.PlayingLocal)
                {
                    player.ApplyKnockbackLocal(OwnerID.Value, direction, knockback, damage, isCrit);
                    CreateSlimePuddleLocal(transform.position);
                }
                else
                {
                    player.ApplyKnockbackServerRpc(OwnerID.Value, direction, knockback, damage, isCrit);
                    CreateSlimePuddleServerRpc(transform.position);
                }
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
        puddleTrail?.StopTrail();

        puddle.GetComponent<NetworkObject>()?.Spawn();
    }

    private void CreateSlimePuddleLocal(Vector3 position)
    {
        Vector3 puddlePos = new Vector3(position.x, 0.06f, position.z);
        GameObject puddle = Instantiate(slimePuddleObject, puddlePos, Quaternion.LookRotation(transform.forward));
        SlimeTrail puddleTrail = puddle.GetComponent<SlimeTrail>();
        puddleTrail?.StopTrail();
    }

    public override void SetSlippy()
    {
        // Left deliberately empty
    }
}