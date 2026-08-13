using Unity.Netcode;
using UnityEngine;

public class CrossBubbler : BasicBubble
{
    [Header("Special Stats")]
    [SerializeField] private float crossPointOffset = 5f;
    [SerializeField] private float sideOffset = 5f;
    [SerializeField] private GameObject crossBubble;

    private Vector3 crossPoint;

    public override void InitialiseBubble(int ID, Vector3 dir, Collider playerCollider, int assignedSpellID, bool fakeWithServerCaster)
    {
        base.InitialiseBubble(ID, dir, playerCollider, assignedSpellID, fakeWithServerCaster);

        // Ensure AssignedSpellID is explicitly set before sub-shots inherit it
        OwnerID.Value = ID;
        AssignedSpellID.Value = assignedSpellID;
        direction = dir;
        crossPoint = transform.position + (direction * crossPointOffset);
        this.playerCollider = playerCollider;
        transform.position = playerCollider.transform.position;

        SpawnCrossShots();
    }

    private void SpawnCrossShots()
    {
        // --- 1. RIGHT CROSS BUBBLE ---
        Vector3 spawnPositionRight = transform.position + (transform.right * sideOffset);
        Vector3 crossDirectionRight = (crossPoint - spawnPositionRight).normalized;

        GameObject bubbleRight = Instantiate(crossBubble, spawnPositionRight, Quaternion.LookRotation(crossDirectionRight));
        BasicBubble scriptRight = bubbleRight.GetComponent<BasicBubble>();

        if (isLocalFake)
        {
            var netObj = bubbleRight.GetComponent<NetworkObject>();
            if (netObj != null) DestroyImmediate(netObj);

            if (scriptRight != null)
            {
                scriptRight.isLocalFake = true;
            }
        }
        else if (IsServer)
        {
            NetworkObject netObj = bubbleRight.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();
        }

        if (scriptRight != null)
        {
            // ID offset + 1 ensures unique Spell ID matching for TryLinkLocalFake
            scriptRight.InitialiseBubble(OwnerID.Value, crossDirectionRight, playerCollider, AssignedSpellID.Value + 1, fakeWithServerCaster);
        }

        // --- 2. LEFT CROSS BUBBLE ---
        Vector3 spawnPositionLeft = transform.position + (transform.right * -sideOffset);
        Vector3 crossDirectionLeft = (crossPoint - spawnPositionLeft).normalized;

        GameObject bubbleLeft = Instantiate(crossBubble, spawnPositionLeft, Quaternion.LookRotation(crossDirectionLeft));
        BasicBubble scriptLeft = bubbleLeft.GetComponent<BasicBubble>();

        if (isLocalFake)
        {
            var netObj = bubbleLeft.GetComponent<NetworkObject>();
            if (netObj != null) DestroyImmediate(netObj);

            if (scriptLeft != null)
            {
                scriptLeft.isLocalFake = true;
            }
        }
        else if (IsServer)
        {
            NetworkObject netObj = bubbleLeft.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();
        }

        if (scriptLeft != null)
        {
            // ID offset + 2 ensures unique Spell ID matching for TryLinkLocalFake
            scriptLeft.InitialiseBubble(OwnerID.Value, crossDirectionLeft, playerCollider, AssignedSpellID.Value + 2, fakeWithServerCaster);
        }

        // --- 3. CLEANUP SPAWNER ---
        if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn(true);
        }

        Destroy(gameObject);
    }

    // Spawner bubble never moves or detects impacts directly
    protected override bool DetectsImpact(out Vector3 impactPoint)
    {
        impactPoint = transform.position;
        return false;
    }
}