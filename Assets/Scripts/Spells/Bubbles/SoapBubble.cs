using UnityEngine;
using Unity.Netcode;

public class SoapBubble : BasicBubble
{
    [Header("Special Stats")]
    [SerializeField] private float soapDropInterval = 0.2f;
    [SerializeField] private GameObject soapPuddleObject;
    [SerializeField] private GameObject fakeSoapPuddleObject;
    [SerializeField] private GameObject soapSplatObject;
    [SerializeField] private GameObject fakeSoapSplatObject;
    [SerializeField] private LayerMask groundedLayerMask;

    private const float raycastDistance = 5f;
    private float timer = 0f;

    protected override void BubbleMovement()
    {
        if (!IsServer && !isLocalFake) return;

        base.BubbleMovement();

        if (soapPuddleObject == null) return;

        timer += Time.fixedDeltaTime;
        if (timer >= soapDropInterval)
        {
            DropSoapPuddle(false);
            timer = 0f;
        }
    }

    private void DropSoapPuddle(bool hitPlayer)
    {
        if (!IsServer && !isLocalFake) return;

        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo, raycastDistance, groundedLayerMask))
        {
            if (IsServer)
            {
                GameObject prefabToSpawn = hitPlayer ? soapSplatObject : soapPuddleObject;
                if (prefabToSpawn == null) return;

                GameObject puddle = Instantiate(prefabToSpawn, hitInfo.point, transform.rotation);
                puddle.GetComponent<NetworkObject>()?.Spawn();
                puddle.GetComponent<DamageField>()?.SetID(OwnerID.Value);

                Puddle puddleScript = puddle.GetComponent<Puddle>();
                if (puddleScript != null)
                {
                    puddleScript.InitialisePuddle(playerCollider);
                }
            }
            else if (isLocalFake)
            {
                GameObject prefabToSpawn = hitPlayer ? fakeSoapSplatObject : fakeSoapPuddleObject;
                if (prefabToSpawn == null) return;

                GameObject puddle = Instantiate(prefabToSpawn, hitInfo.point, transform.rotation);

                Puddle puddleScript = puddle.GetComponent<Puddle>();
                if (puddleScript != null)
                {
                    puddleScript.isLocalFake = true;
                }
            }
        }
    }

    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped || other == null) return;
        if (!IsServer && !isLocalFake) return;

        if (other.CompareTag("Player"))
        {
            DropSoapPuddle(true);
        }

        if (isLocalFake)
        {
            Pop();
            return;
        }

        base.BubbleCollision(other);
    }

    public override void SetSlippy()
    {
        // Intentionally left empty for SoapBubble
    }
}