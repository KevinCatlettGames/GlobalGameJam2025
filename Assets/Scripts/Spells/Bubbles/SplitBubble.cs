using FMODUnity;
using Unity.Netcode;
using UnityEngine;

public class SplitBubble : BasicBubble
{
    [Header("Special Stats")]
    [SerializeField] private GameObject bubblePrefab;

    protected override void Pop()
    {
        // Split right
        Vector3 splitDirection = transform.right;
        GameObject bubble = Instantiate(bubblePrefab, transform.position + splitDirection, Quaternion.LookRotation(splitDirection));
        NetworkObject netObj = bubble.GetComponent<NetworkObject>();
        if (netObj != null)
            netObj.Spawn();
        BasicBubble bubbleScript = bubble.GetComponent<BasicBubble>();
        bubbleScript.InitialiseBubble(OwnerID, splitDirection, playerCollider);

        // Split left
        splitDirection = -transform.right;
        bubble = Instantiate(bubblePrefab, transform.position + splitDirection, Quaternion.LookRotation(splitDirection));
        netObj = bubble.GetComponent<NetworkObject>();
        if (netObj != null)
            netObj.Spawn();
        bubbleScript = bubble.GetComponent<BasicBubble>();
        bubbleScript.InitialiseBubble(OwnerID, splitDirection, playerCollider);

        base.Pop();
    }
}
