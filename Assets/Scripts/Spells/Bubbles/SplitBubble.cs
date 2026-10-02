using Unity.Netcode;
using UnityEngine;

public class SplitBubble : BasicBubble
{
    [Header("Special Stats")]
    [SerializeField] private GameObject bubblePrefab;
    [SerializeField] private float offsetAngle = 30f;
    [SerializeField] private float offsetDistance = 2f;

    protected override void Pop()
    {
        if (hasPopped) return;

        if (!hasHitPlayer)
            offsetDistance = .75f;

        SpawnChildPair();

        base.Pop();
    }

    private void SpawnChildPair()
    {
        if (bubblePrefab == null) return;

        SplitTracker sharedTracker = new SplitTracker();

        GameObject bubbleA = SpawnSingleChild(offsetAngle, subIdOffset: 1);
        GameObject bubbleB = SpawnSingleChild(-offsetAngle, subIdOffset: 2);

        if (bubbleA != null && bubbleA.TryGetComponent<SplitAchievementHandler>(out var hA))
        {
            hA.tracker = sharedTracker;
        }

        if (bubbleB != null && bubbleB.TryGetComponent<SplitAchievementHandler>(out var hB))
        {
            hB.tracker = sharedTracker;
        }
    }

    private GameObject SpawnSingleChild(float angle, int subIdOffset)
    {
        Vector3 splitDirection = Quaternion.AngleAxis(angle, Vector3.up) * direction;
        Vector3 spawnPosition = transform.position + (splitDirection * offsetDistance);

        GameObject bubble = Instantiate(bubblePrefab, spawnPosition, Quaternion.LookRotation(splitDirection));
        BasicBubble bubbleScript = bubble.GetComponent<BasicBubble>();

        if (IsServer)
        {
            NetworkObject netObj = bubble.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();
        }
        else if (isLocalFake)
        {
            if (bubble.TryGetComponent<NetworkObject>(out var netObj))
                Destroy(netObj);
        }

        if (bubbleScript != null)
        {
            int uniqueChildID = (AssignedSpellID.Value * 10) + subIdOffset;
            bubbleScript.InitialiseBubble(OwnerID.Value, splitDirection, playerCollider, uniqueChildID, fakeWithServerCaster);
        }

        return bubble;
    }
}