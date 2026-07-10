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

        // Spawn left and right split directions
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

        // --- CLIENT-SIDE PREDICTION & SERVER SPAWNING GATES ---
        if (isLocalFake)
        {
            // Clean up network components on the local client prediction replica
            if (bubble.TryGetComponent<NetworkObject>(out var netObj))
            {
                Destroy(netObj);
            }

            // Bind the child to the predictive tracking layer
            bubble.layer = LayerMask.NameToLayer("FakeProjectiles");

            if (bubbleScript != null)
            {
                bubbleScript.isLocalFake = true;

                // Register with the local player controller so it can clean up if a desync correction happens
                var playerCtrl = playerCollider?.GetComponent<PlayerController>();
                if (playerCtrl != null) playerCtrl.RegisterLocalFake(bubbleScript);
            }
        }
        else if (IsServer)
        {
            NetworkObject netObj = bubble.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();

            if (bubbleScript != null)
            {
                bubbleScript.castID = this.castID; // Tie server instances to the parent's cast lifecycle
            }
        }

        // Initialize physics tracking variables across both execution branches
        if (bubbleScript != null)
        {
            bubbleScript.InitialiseBubble(OwnerID, splitDirection, playerCollider);
        }
    }
}