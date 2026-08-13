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
            offsetDistance = 0.75f;

        // Ensure child bubbles are spawned only on server or local fake prediction instances
        if (IsServer || isLocalFake)
        {
            SpawnChildPair();
        }

        base.Pop();
    }

    private void SpawnChildPair()
    {
        if (bubblePrefab == null) return;

        SplitTracker sharedTracker = new SplitTracker();

        GameObject bubbleA = SpawnSingleChild(offsetAngle);
        GameObject bubbleB = SpawnSingleChild(-offsetAngle);

        if (bubbleA != null)
        {
            var hA = bubbleA.GetComponent<SplitAchievementHandler>();
            if (hA != null) hA.tracker = sharedTracker;
        }

        if (bubbleB != null)
        {
            var hB = bubbleB.GetComponent<SplitAchievementHandler>();
            if (hB != null) hB.tracker = sharedTracker;
        }
    }

    private GameObject SpawnSingleChild(float angle)
    {
        Vector3 splitDirection = Quaternion.AngleAxis(angle, Vector3.up) * direction;
        Vector3 spawnPosition = transform.position + (splitDirection * offsetDistance);

        GameObject bubble = Instantiate(bubblePrefab, spawnPosition, Quaternion.LookRotation(splitDirection));
        BasicBubble bubbleScript = bubble.GetComponent<BasicBubble>();

        if (IsServer)
        {
            NetworkObject netObj = bubble.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                netObj.Spawn();
            }
        }
        else if (isLocalFake)
        {
            NetworkObject netObj = bubble.GetComponent<NetworkObject>();
            if (netObj != null)
            {
                Destroy(netObj);
            }
        }

        if (bubbleScript != null)
        {
            bubbleScript.InitialiseBubble(OwnerID.Value, splitDirection, playerCollider, AssignedSpellID.Value + 1, fakeWithServerCaster);
        }

        return bubble;
    }
}