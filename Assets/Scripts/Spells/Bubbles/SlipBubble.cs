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

                NetworkObject netObj = trail.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    Destroy(netObj);
                }

                slimeTrail = trail.GetComponent<SlimeTrail>();
                slimeTrail?.InitialiseTrail(speed);
            }
        }
        else if (IsServer)
        {
            if (slimeTrailObject != null)
            {
                GameObject trail = Instantiate(slimeTrailObject, trailPos, Quaternion.LookRotation(transform.forward));
                slimeTrail = trail.GetComponent<SlimeTrail>();
                slimeTrail?.InitialiseTrail(speed);

                trail.GetComponent<NetworkObject>()?.Spawn();
            }
        }
    }

    protected override void BubbleMovement()
    {
        base.BubbleMovement();

        if (!IsServer && !isLocalFake) return;

        // Stop trail emission if bubble drifts off grounded geometry
        if (slimeTrail != null && !slimeTrail.isStopped)
        {
            if (!Physics.Raycast(transform.position, Vector3.down, 5f, groundedLayerMask))
            {
                slimeTrail.StopTrail();
            }
        }
    }

    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped || other == null) return;
        if (!IsServer && !isLocalFake) return;

        // Local fake collision branch
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

        // Server authoritative collision branch
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            if (player != null)
            {
                if (GameManager.Instance.PlayingLocal)
                {
                    player.ApplyKnockbackLocal(OwnerID.Value, direction, knockback, damage);
                }
                else
                {
                    player.ApplyKnockbackServerRpc(OwnerID.Value, direction, knockback, damage);
                }

                CreateSlimePuddleServer(transform.position);
            }

            Pop();
        }
    }

    protected override void Pop()
    {
        if (hasPopped) return;

        if (slimeTrail != null)
        {
            slimeTrail.StopTrail();

            if (IsServer)
            {
                NetworkObject trailNetObj = slimeTrail.gameObject.GetComponent<NetworkObject>();
                if (trailNetObj != null && trailNetObj.IsSpawned)
                {
                    trailNetObj.Despawn(true);
                }
            }
            else if (isLocalFake)
            {
                Destroy(slimeTrail.gameObject);
            }
        }

        base.Pop();
    }

    private void CreateSlimePuddleServer(Vector3 position)
    {
        if (!IsServer || slimePuddleObject == null) return;

        Vector3 puddlePos = new Vector3(position.x, 0.06f, position.z);
        GameObject puddle = Instantiate(slimePuddleObject, puddlePos, Quaternion.LookRotation(transform.forward));

        SlimeTrail puddleTrail = puddle.GetComponent<SlimeTrail>();
        puddleTrail?.StopTrail();

        puddle.GetComponent<NetworkObject>()?.Spawn();
    }

    private void CreateSlimePuddleLocal(Vector3 position)
    {
        if (slimePuddleObject == null) return;

        Vector3 puddlePos = new Vector3(position.x, 0.06f, position.z);
        GameObject puddle = Instantiate(slimePuddleObject, puddlePos, Quaternion.LookRotation(transform.forward));

        NetworkObject netObj = puddle.GetComponent<NetworkObject>();
        if (netObj != null)
        {
            Destroy(netObj);
        }

        SlimeTrail puddleTrail = puddle.GetComponent<SlimeTrail>();
        puddleTrail?.StopTrail();
    }

    public override void SetSlippy()
    {
        // Intentionally left empty for SlipBubble
    }
}