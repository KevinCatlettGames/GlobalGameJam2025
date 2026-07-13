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
        int baseCastID = this.castID;

        int rightBulletID = (baseCastID * 10) + 1;
        int leftBulletID = (baseCastID * 10) + 2;

        Vector3 spawnPosition = transform.position + (transform.right * sideOffset);
        Vector3 crossDirection = (crossPoint - spawnPosition).normalized;

        GameObject bubbleRight = Instantiate(crossBubble, spawnPosition, Quaternion.LookRotation(crossDirection));
        BasicBubble scriptRight = bubbleRight.GetComponent<BasicBubble>();

        if (isLocalFake)
        {
            Destroy(bubbleRight.GetComponent<NetworkObject>());
            bubbleRight.layer = LayerMask.NameToLayer("FakeProjectiles");

            if (scriptRight != null)
            {
                scriptRight.isLocalFake = true;
                scriptRight.castID = rightBulletID;

                var playerCtrl = playerCollider?.GetComponent<PlayerController>();
                if (playerCtrl != null) playerCtrl.RegisterLocalFake(scriptRight);
            }
        }
        else if (IsServer)
        {
            if (scriptRight != null)
            {
                scriptRight.syncedCastID.Value = rightBulletID;
                scriptRight.castID = rightBulletID;
            }

            NetworkObject netObj = bubbleRight.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();
        }

        if (scriptRight != null)
        {
            scriptRight.InitialiseBubble(OwnerID, crossDirection, playerCollider);
        }


        spawnPosition = transform.position + (transform.right * -sideOffset);
        crossDirection = (crossPoint - spawnPosition).normalized;

        GameObject bubbleLeft = Instantiate(crossBubble, spawnPosition, Quaternion.LookRotation(crossDirection));
        BasicBubble scriptLeft = bubbleLeft.GetComponent<BasicBubble>();


        if (IsServer)
        {
            if (scriptLeft != null)
            {
                scriptLeft.syncedCastID.Value = leftBulletID;
                scriptLeft.castID = leftBulletID;
            }

            NetworkObject netObj = bubbleLeft.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();
        }
        else if (isLocalFake)
        {
            Destroy(bubbleLeft.GetComponent<NetworkObject>());
            bubbleLeft.layer = LayerMask.NameToLayer("FakeProjectiles");

            if (scriptLeft != null)
            {
                scriptLeft.isLocalFake = true;
                scriptLeft.castID = leftBulletID;

                var playerCtrl = playerCollider?.GetComponent<PlayerController>();
                if (playerCtrl != null) playerCtrl.RegisterLocalFake(scriptLeft);
            }
        }

        if (scriptLeft != null)
        {
            scriptLeft.InitialiseBubble(OwnerID, crossDirection, playerCollider);
        }

        Destroy(gameObject);
    }
}