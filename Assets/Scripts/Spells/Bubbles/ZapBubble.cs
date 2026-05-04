using FMODUnity;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using static UnityEngine.UI.Image;

public class ZapBubble : BasicBubble
{
    [Header("SpecialStats")]
    [SerializeField] private float delayBetweenZaps = .08f;
    [SerializeField] private float initialDelay = .2f;
    [SerializeField] private int zaps = 3;
    [SerializeField] private GameObject bubblePrefab;
    private Vector3 offset;
    [SerializeField] private EventReference zapSoundEvent;

    public override void InitialiseBubble(int ID, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        OwnerID = ID;
        direction = dir;
        offset = transform.position - playerCollider.transform.position;
        this.playerCollider = playerCollider;
        RuntimeManager.PlayOneShotAttached(soundEvent, gameObject);
        StartCoroutine(ZapCoroutine());
    }
    protected override void BubbleMovement()
    {
        transform.position = playerCollider.transform.position + offset;
    }
    private IEnumerator ZapCoroutine()
    {
        RuntimeManager.PlayOneShotAttached(zapSoundEvent, gameObject);
        yield return new WaitForSeconds(initialDelay);
        for (int i = 0; i < zaps; i++)
        {
            GameObject bubbleObj = Instantiate(bubblePrefab, transform.position, Quaternion.LookRotation(direction));
            NetworkObject netObj = bubbleObj.GetComponent<NetworkObject>();
            if (netObj != null)
                netObj.Spawn();

            BasicBubble bubbleScript = bubbleObj.GetComponent<BasicBubble>();
            bubbleScript.InitialiseBubble(OwnerID, direction, zapSoundEvent, playerCollider);
            
            yield return new WaitForSeconds(delayBetweenZaps);
        }
        Pop();
    }
}
