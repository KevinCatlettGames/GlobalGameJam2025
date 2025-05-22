using System.Collections;
using UnityEngine;

public class SoapBubble : BasicBubble
{
    [SerializeField] private GameObject soapPuddleObject;
    [SerializeField] private float soapDropIntervall = .2f;
    [SerializeField] private LayerMask groundedLayerMask;

    private void Start()
    {
        if (soapPuddleObject != null) StartCoroutine(SoapDropCoroutine());
    }

    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped) return;
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            player.ApplyKnockback(OwnerID, direction, knockback, damage);
            RaycastHit hitInfo;
            if(Physics.Raycast(transform.position, Vector3.up * -1, out hitInfo, 5f, groundedLayerMask))
                Instantiate(soapPuddleObject, hitInfo.point, transform.rotation);
        }
        Pop();
    }
    public override void SetSlippy()
    {
        return;
    }

    private IEnumerator SoapDropCoroutine()
    {
        yield return new WaitForSeconds(soapDropIntervall);
        while (!hasPopped)
        {
            RaycastHit hitInfo;
            if (Physics.Raycast(transform.position, Vector3.up * -1, out hitInfo, 5f, groundedLayerMask))
                Instantiate(soapPuddleObject, hitInfo.point, transform.rotation);
            yield return new WaitForSeconds(soapDropIntervall);
        }
    } 
}
