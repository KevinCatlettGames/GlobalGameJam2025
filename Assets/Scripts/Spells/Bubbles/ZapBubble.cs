using FMODUnity;
using System.Collections;
using UnityEngine;
using static UnityEngine.UI.Image;

public class ZapBubble : BasicBubble
{
    [Header("SpecialStats")]
    [SerializeField] private float delayBetweenZaps = .08f;
    [SerializeField] private int zaps = 3;
    [SerializeField] private float[] damages;
    [SerializeField] private float[] knockbacks;
    [SerializeField] private LayerMask zapLayerMask;
    private Vector3 offset;
    [SerializeField] private EventReference zapSoundEvent;

    public override void InitialiseBubble(int ID, Vector3 dir, EventReference soundEvent, Collider playerCollider)
    {
        OwnerID = ID;
        direction = dir;
        offset = transform.position - playerCollider.transform.position;
        this.zapSoundEvent = soundEvent;
        this.playerCollider = playerCollider;
        StartCoroutine(ZapCoroutine());
    }
    protected override void BubbleMovement()
    {
        transform.position = playerCollider.transform.position + offset;
    }
    private IEnumerator ZapCoroutine()
    {
        for (int i = 0; i < zaps; i++)
        {
            Zap(damages[i], knockbacks[i]);
            yield return new WaitForSeconds(delayBetweenZaps);
        }
        Pop();
    }
    private void Zap(float _dmg, float _knb)
    {
        RuntimeManager.PlayOneShotAttached(zapSoundEvent, gameObject);
        RaycastHit[] hits = Physics.RaycastAll(transform.position, direction, range, zapLayerMask);
        Debug.DrawRay(transform.position, direction * range, Color.red, .1f);
        if (hits.Length != 0)
        {
            RaycastHit hit;
            for (int i = 0; i < hits.Length; i++)
            {
                hit = hits[i];
                if (hit.transform.CompareTag("Player"))
                {
                    PlayerController player = hit.transform.GetComponent<PlayerController>();
                    player?.ApplyKnockbackLocal(OwnerID, direction, _knb, _dmg);
                    if (!isUlt) 
                        playerCollider.GetComponent<PlayerController>().GainUltCharge(_dmg, true);
                }
                else if (hit.transform.CompareTag("Bubble"))
                {
                    BasicBubble bubble = hit.transform.GetComponent<BasicBubble>();
                    bubble?.BubbleCollision(this.gameObject);
                }
                else if (hit.transform.CompareTag("Wall"))
                {
                    break;
                }
            }
        }
    }
}
