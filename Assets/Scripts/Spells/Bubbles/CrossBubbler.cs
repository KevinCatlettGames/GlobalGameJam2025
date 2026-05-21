using FMODUnity;
using Unity.Netcode;
using UnityEngine;
using static UnityEditor.PlayerSettings;

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
        // Spawn right
        Vector3 spawnPosition = transform.position + (transform.right * sideOffset);
        Vector3 crossDirection = crossPoint - spawnPosition;
        crossDirection.Normalize();

        GameObject bubble = Instantiate(crossBubble, spawnPosition, Quaternion.LookRotation(crossDirection));
        NetworkObject netObj = bubble.GetComponent<NetworkObject>();
        if (netObj != null)
            netObj.Spawn();

        BasicBubble bubbleScript = bubble.GetComponent<BasicBubble>();
        bubbleScript.InitialiseBubble(OwnerID, crossDirection, playerCollider);


        // Spawn left
        spawnPosition = transform.position + (transform.right * -sideOffset);
        crossDirection = crossPoint - spawnPosition;
        crossDirection.Normalize();

        bubble = Instantiate(crossBubble, spawnPosition, Quaternion.LookRotation(crossDirection));
        netObj = bubble.GetComponent<NetworkObject>();
        if (netObj != null)
            netObj.Spawn();

        bubbleScript = bubble.GetComponent<BasicBubble>();
        bubbleScript.InitialiseBubble(OwnerID, crossDirection, playerCollider);

        Destroy(gameObject);
    }
}
