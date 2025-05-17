using UnityEngine;

public class SoapBubble : BasicBubble
{
    [SerializeField] private GameObject soapPuddleObject;
    [SerializeField] private float soapDropIntervall = .2f;
    [SerializeField] private LayerMask groundedLayerMask;
    private float timer = 0f;

    private void Update()
    {
        if (soapPuddleObject != null)
        {
            timer += Time.deltaTime;
            if (timer >= soapDropIntervall)
            {
                RaycastHit hitInfo;
                if (Physics.Raycast(transform.position, Vector3.up * -1, out hitInfo, 5f, groundedLayerMask))
                    Instantiate(soapPuddleObject, hitInfo.point, transform.rotation);
                timer = 0f;
            }
        }
    }

    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped.Value) return;
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            player.ApplyKnockbackServerRpc(OwnerID.Value, direction.Value, knockback, damage);
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
}
