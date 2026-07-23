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
        OwnerID.Value = ID;
        direction = dir;
        crossPoint = transform.position + (direction * crossPointOffset);
        this.playerCollider = playerCollider;
        transform.position = playerCollider.transform.position;
        SpawnCrossShots();
    }

    private void SpawnCrossShots()
    {
        Vector3 spawnPosition = transform.position + (transform.right * sideOffset);
        Vector3 crossDirection = (crossPoint - spawnPosition).normalized;

        GameObject bubbleRight = Instantiate(crossBubble, spawnPosition, Quaternion.LookRotation(crossDirection));
        BasicBubble scriptRight = bubbleRight.GetComponent<BasicBubble>();

        if (isLocalFake)
        {
            Destroy(bubbleRight.GetComponent<NetworkObject>());

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
            scriptRight.InitialiseBubble(OwnerID.Value, crossDirection, playerCollider, AssignedSpellID.Value+1, fakeWithServerCaster);
        }


        spawnPosition = transform.position + (transform.right * -sideOffset);
        crossDirection = (crossPoint - spawnPosition).normalized;

        GameObject bubbleLeft = Instantiate(crossBubble, spawnPosition, Quaternion.LookRotation(crossDirection));
        BasicBubble scriptLeft = bubbleLeft.GetComponent<BasicBubble>();


        if (IsServer)
        {
            NetworkObject netObj = bubbleLeft.GetComponent<NetworkObject>();
            if (netObj != null) netObj.Spawn();
        }
        else if (isLocalFake)
        {
            Destroy(bubbleLeft.GetComponent<NetworkObject>());

            if (scriptLeft != null)
            {
                scriptLeft.isLocalFake = true;

            }
        }

        if (scriptLeft != null)
        {
            scriptLeft.InitialiseBubble(OwnerID.Value, crossDirection, playerCollider, AssignedSpellID.Value+2, fakeWithServerCaster);
        }

        Destroy(gameObject);
    }
}