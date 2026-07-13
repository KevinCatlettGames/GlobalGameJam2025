using FMODUnity;
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

        SpawnChildBubble(offsetAngle);
        SpawnChildBubble(-offsetAngle);

        base.Pop();
    }

    private void SpawnChildBubble(float angle)
    {
        if (bubblePrefab == null) return;

        Vector3 splitDirection = Quaternion.AngleAxis(angle, Vector3.up) * direction;
        Vector3 spawnPosition = transform.position + (splitDirection * offsetDistance);

        GameObject bubble = Instantiate(bubblePrefab, spawnPosition, Quaternion.LookRotation(splitDirection));
        BasicBubble bubbleScript = bubble.GetComponent<BasicBubble>();

        if (IsServer)
        {
            NetworkObject netObj = bubble.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();

            if (bubbleScript != null)
                bubbleScript.castID = this.castID;
        }
        else if (isLocalFake)
        {
            if (bubble.TryGetComponent<NetworkObject>(out var netObj))
                Destroy(netObj);

            bubble.layer = LayerMask.NameToLayer("FakeProjectiles");

            if (bubbleScript != null)
            {
                bubbleScript.isLocalFake = true;
                var playerCtrl = playerCollider?.GetComponent<PlayerController>();
                if (playerCtrl != null) playerCtrl.RegisterLocalFake(bubbleScript);
            }
        }

        if (bubbleScript != null)
            bubbleScript.InitialiseBubble(OwnerID, splitDirection, playerCollider);
    }
}