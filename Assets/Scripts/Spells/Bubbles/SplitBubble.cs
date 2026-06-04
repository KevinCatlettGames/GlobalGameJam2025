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
        Vector3 splitDirection = direction;
        if (!hasHitPlayer)
            offsetDistance = .75f;

        GameObject bubble = Instantiate(bubblePrefab, transform.position + splitDirection * offsetDistance, Quaternion.LookRotation(splitDirection));
        NetworkObject netObj = bubble.GetComponent<NetworkObject>();
        if (netObj != null)
            netObj.Spawn();
        BasicBubble bubbleScript = bubble.GetComponent<BasicBubble>();
        bubbleScript.InitialiseBubble(OwnerID, splitDirection, playerCollider);


        // Split right
        splitDirection = Quaternion.AngleAxis(offsetAngle, Vector3.up) * direction;
        bubble = Instantiate(bubblePrefab, transform.position + splitDirection * offsetDistance, Quaternion.LookRotation(splitDirection));
        netObj = bubble.GetComponent<NetworkObject>();
        if (netObj != null)
            netObj.Spawn();
        bubbleScript = bubble.GetComponent<BasicBubble>();
        bubbleScript.InitialiseBubble(OwnerID, splitDirection, playerCollider);

        // Split left
        splitDirection = Quaternion.AngleAxis(-offsetAngle, Vector3.up) * direction;
        bubble = Instantiate(bubblePrefab, transform.position + splitDirection * offsetDistance, Quaternion.LookRotation(splitDirection));
        netObj = bubble.GetComponent<NetworkObject>();
        if (netObj != null)
            netObj.Spawn();
        bubbleScript = bubble.GetComponent<BasicBubble>();
        bubbleScript.InitialiseBubble(OwnerID, splitDirection, playerCollider);

        base.Pop();
    }
}
