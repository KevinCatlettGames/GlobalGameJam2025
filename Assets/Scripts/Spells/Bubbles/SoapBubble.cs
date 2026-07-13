using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class SoapBubble : BasicBubble
{
    [Header("Special Stats")]
    [SerializeField] private float soapDropInterval = 0.2f;
    [SerializeField] private GameObject soapPuddleObject;
    [SerializeField] private GameObject soapSplatObject;
    [SerializeField] private LayerMask groundedLayerMask;
    private const float raycastDistance = 5f;

    private float timer = 0;
    protected override void Update()
    {
        base.Update();

        if (!IsServer && !isLocalFake) return;
        if (soapPuddleObject == null) return;

        timer += Time.deltaTime;
        if (timer >= soapDropInterval)
        {
            DropSoapPuddle(false);
            timer = 0f;
        }
    }

    private void DropSoapPuddle(bool hitPlayer)
    {
        if (IsServer)
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo, raycastDistance, groundedLayerMask))
            {
                GameObject prefabToSpawn = hitPlayer ? soapSplatObject : soapPuddleObject;
                if (prefabToSpawn == null) return;
                GameObject puddle = Instantiate(prefabToSpawn, hitInfo.point, transform.rotation);
                puddle.GetComponent<NetworkObject>()?.Spawn();
            }
        }
    }

    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped || other == null) return;

        if (IsServer && other.CompareTag("Player"))
            DropSoapPuddle(true);

        base.BubbleCollision(other);
    }

    public override void SetSlippy()
    {
        return;
    }
}