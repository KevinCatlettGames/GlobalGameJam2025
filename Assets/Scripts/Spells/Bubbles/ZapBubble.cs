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

    public override void InitialiseBubble(int ID, Vector3 dir, Collider playerCollider, int assignedSpellID, bool fakeWithServerCaster)
    {
        OwnerID.Value = ID;
        direction = dir;
        this.playerCollider = playerCollider;

        if (playerCollider != null)
        {
            offset = transform.position - playerCollider.transform.position;
        }

        if (isLocalFake || IsServer)
        {
            RuntimeManager.PlayOneShotAttached(soundEvent, gameObject);
        }

        StartCoroutine(ZapCoroutine());
    }

    protected override void BubbleMovement()
    {
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

            if (isLocalFake)
            {
                if (bubbleObj.TryGetComponent<NetworkObject>(out var netObj))
                {
                    Destroy(netObj);
                }


                if (bubbleScript != null)
                {
                    bubbleScript.isLocalFake = true;

                }
            }
            else if (IsServer)
            {
                NetworkObject netObj = bubbleObj.GetComponent<NetworkObject>();
                if (netObj != null) netObj.Spawn();
            }

            if (bubbleScript != null)
            {
                bubbleScript.InitialiseBubble(OwnerID.Value, direction, playerCollider, AssignedSpellID.Value + 1, fakeWithServerCaster);
            }

            yield return new WaitForSeconds(delayBetweenZaps);
        }

        Pop();
    }
}