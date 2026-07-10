using Unity.Netcode;
using UnityEngine;

public class CrossBubbler : BasicBubble
{
    [Header("Special Stats")]
    [SerializeField] private float crossPointOffset = 5f;
    [SerializeField] private float sideOffset = 5f;
    [SerializeField] private GameObject crossBubble;

    private Vector3 crossPoint;

    public override void InitialiseBubble(int ID, Vector3 dir, Collider playerCollider)
    {
        OwnerID = ID;
        direction = dir;
        crossPoint = transform.position + (direction * crossPointOffset);
        this.playerCollider = playerCollider;
        transform.position = playerCollider.transform.position;

        SpawnCrossShots();
    }

    private void SpawnCrossShots()
    {
        // --- 1. SPAWN RIGHT ---
        Vector3 spawnPosition = transform.position + (transform.right * sideOffset);
        Vector3 crossDirection = crossPoint - spawnPosition;
        crossDirection.Normalize();

        GameObject bubbleRight = Instantiate(crossBubble, spawnPosition, Quaternion.LookRotation(crossDirection));
        BasicBubble scriptRight = bubbleRight.GetComponent<BasicBubble>();

        if (isLocalFake)
        {
            // Set up client-side prediction properties
            Destroy(bubbleRight.GetComponent<NetworkObject>());
            bubbleRight.layer = LayerMask.NameToLayer("FakeProjectiles");
            scriptRight.isLocalFake = true;

            // Generate a unique GUID tracker for this sub-bubble and register it to the player controller
        
            var playerCtrl = playerCollider?.GetComponent<PlayerController>();
            if (playerCtrl != null) playerCtrl.RegisterLocalFake(scriptRight);
        }
        else if (IsServer)
        {
            NetworkObject netObj = bubbleRight.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();

            // Note: If you pass down the parent CrossBubbler's unique castID to its children,
            // make sure to handle individual tracking logic if needed.
            scriptRight.castID = this.castID;
        }

        scriptRight.InitialiseBubble(OwnerID, crossDirection, playerCollider);


        // --- 2. SPAWN LEFT ---
        spawnPosition = transform.position + (transform.right * -sideOffset);
        crossDirection = crossPoint - spawnPosition;
        crossDirection.Normalize();

        GameObject bubbleLeft = Instantiate(crossBubble, spawnPosition, Quaternion.LookRotation(crossDirection));
        BasicBubble scriptLeft = bubbleLeft.GetComponent<BasicBubble>();

        if (isLocalFake)
        {
            // Set up client-side prediction properties
            Destroy(bubbleLeft.GetComponent<NetworkObject>());
            bubbleLeft.layer = LayerMask.NameToLayer("FakeProjectiles");
            scriptLeft.isLocalFake = true;

            // Generate a unique GUID tracker for this sub-bubble and register it to the player controller
            var playerCtrl = playerCollider?.GetComponent<PlayerController>();
            if (playerCtrl != null) playerCtrl.RegisterLocalFake(scriptLeft);
        }
        else if (IsServer)
        {
            NetworkObject netObj = bubbleLeft.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();

            scriptLeft.castID = this.castID;
        }

        scriptLeft.InitialiseBubble(OwnerID, crossDirection, playerCollider);

        // Clean up the factory utility component frame immediately
        Destroy(gameObject);
    }
}