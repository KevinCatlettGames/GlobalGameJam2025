using FMODUnity;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class ZapBubble : BasicBubble
{
    [Header("SpecialStats")]
    [SerializeField] private float delayBetweenZaps = .08f;
    [SerializeField] private float initialDelay = .2f;
    [SerializeField] private int zaps = 3;
    [SerializeField] private GameObject bubblePrefab;
    private Vector3 offset;
    [SerializeField] private EventReference zapSoundEvent;

    public override void InitialiseBubble(int ID, Vector3 dir, Collider playerCollider)
    {
        OwnerID = ID;
        direction = dir;
        this.playerCollider = playerCollider;

        if (playerCollider != null)
        {
            offset = transform.position - playerCollider.transform.position;
        }

        // Play the firing audio instantly on the local client machine
        if (isLocalFake || IsServer)
        {
            RuntimeManager.PlayOneShotAttached(soundEvent, gameObject);
        }

        StartCoroutine(ZapCoroutine());
    }

    protected override void BubbleMovement()
    {
        // Keep the spawner attached to the casting player across both contexts
        if (playerCollider != null)
        {
            transform.position = playerCollider.transform.position + offset;
        }
    }

    private IEnumerator ZapCoroutine()
    {
        if (isLocalFake || IsServer)
        {
            RuntimeManager.PlayOneShotAttached(zapSoundEvent, gameObject);
        }

        yield return new WaitForSeconds(initialDelay);

        for (int i = 0; i < zaps; i++)
        {
            if (bubblePrefab == null) yield break;

            GameObject bubbleObj = Instantiate(bubblePrefab, transform.position, Quaternion.LookRotation(direction));
            BasicBubble bubbleScript = bubbleObj.GetComponent<BasicBubble>();

            // --- CLIENT-SIDE PREDICTION & SERVER SPAWNING GATES ---
            if (isLocalFake)
            {
                // Remove the network object copy from our client-side visualization fake
                if (bubbleObj.TryGetComponent<NetworkObject>(out var netObj))
                {
                    Destroy(netObj);
                }

                // Place the predicted sub-bullet onto the isolated client collision layer
                bubbleObj.layer = LayerMask.NameToLayer("FakeProjectiles");

                if (bubbleScript != null)
                {
                    bubbleScript.isLocalFake = true;

                    // Register with the local player controller for safety tracking
                    var playerCtrl = playerCollider?.GetComponent<PlayerController>();
                    if (playerCtrl != null) playerCtrl.RegisterLocalFake(bubbleScript);
                }
            }
            else if (IsServer)
            {
                NetworkObject netObj = bubbleObj.GetComponent<NetworkObject>();
                if (netObj != null) netObj.Spawn();

                if (bubbleScript != null)
                {
                    bubbleScript.castID = this.castID; // Link sub-projectiles to the server's tracking cast ID
                }
            }

            // Fire and initialize the sub-projectile across both states
            if (bubbleScript != null)
            {
                bubbleScript.InitialiseBubble(OwnerID, direction, playerCollider);
            }

            yield return new WaitForSeconds(delayBetweenZaps);
        }

        Pop();
    }
}