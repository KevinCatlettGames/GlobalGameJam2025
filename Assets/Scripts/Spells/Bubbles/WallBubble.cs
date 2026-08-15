using Unity.Netcode;
using UnityEngine;

public class WallBubble : BasicBubble
{
    [Header("Special Stats")]
    [SerializeField] private float speedBoost = 1.5f;
    [SerializeField] private Material dmgedOutline;
    private int hitPoints = 0;
    private bool stop = false;

    public override void InitialiseBubble(int ID, Vector3 dir, Collider playerCollider, int assignedSpellID, bool fakeWithServerCaster)
    {
        base.InitialiseBubble(ID, dir, playerCollider, assignedSpellID, fakeWithServerCaster);

        Reflector reflector = GetComponent<Reflector>();
        if (reflector != null)
        {
            reflector.OwnerID = ID;
        }
        else
        {
            //Debug.LogWarning("Reflector component missing on WallBubble.");
        }

        canMiss = false;
        hitPoints = Mathf.Max(1, Mathf.RoundToInt(damage));
    }

    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped || other == null) return;
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            return;
        }

        if (other.CompareTag("Bubble") && popOnBubbleHit)
        {
            hitPoints--;

            if (!isLocalFake)
            {
                MeshRenderer renderer = GetComponent<MeshRenderer>();
                if (renderer != null && dmgedOutline != null)
                {
                    Material[] materials = renderer.materials;
                    if (materials.Length > 1)
                    {
                        materials[1] = dmgedOutline;
                        renderer.materials = materials;
                    }
                }
            }

            if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay)
            {
                ChangeMaterialServerRpc();
            }

            BasicBubble otherBubble = other.GetComponent<BasicBubble>();
            if (otherBubble != null)
            {
                otherBubble.ChangeSpeed(speedBoost);
            }

            if (hitPoints <= 0)
            {
                Pop();
            }
        }
        else if (other.CompareTag("Wall"))
        {
            stop = true;
        }
    }

    protected override void BubbleMovement()
    {
        if (!IsServer && !isLocalFake) return;

        if (!stop)
            base.BubbleMovement();
    }

    public void ChangeMaterial()
    {
        MeshRenderer renderer = GetComponent<MeshRenderer>();

        if (isLocalFake)
            renderer = GetComponentInChildren<MeshRenderer>();

        if (renderer != null && dmgedOutline != null)
        {
            Material[] materials = renderer.materials;
            if (materials.Length > 1)
            {
                materials[1] = dmgedOutline;
                renderer.materials = materials;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChangeMaterialServerRpc()
    {
        ChangeMaterialClientRpc();
    }

    [ClientRpc]
    private void ChangeMaterialClientRpc()
    {
        ChangeMaterial();

        if (!IsServer || isLocalFake)
        {
            WallBubble[] allBubbles = FindObjectsByType<WallBubble>(FindObjectsSortMode.None);
            foreach (var bubble in allBubbles)
            {
                if (bubble.isLocalFake && bubble.AssignedSpellID.Value == this.AssignedSpellID.Value)
                {
                    bubble.ChangeMaterial();
                    break;
                }
            }
        }
    }
}