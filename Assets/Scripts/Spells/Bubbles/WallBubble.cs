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

        canMiss = false;
        hitPoints = Mathf.Max(1, Mathf.RoundToInt(damage));
    }

    public override void HandleTrigger(Collider other)
    {
        if (hasPopped || other == null) return;

        if (other.CompareTag("Player"))
        {
            return;
        }

        if (other.CompareTag("Bubble") && popOnBubbleHit)
        {
            // Ignore collisions between wall segments
            if (other.TryGetComponent<WallBubble>(out _))
            {
                return;
            }

            hitPoints--;
            ApplyDamagedMaterial();

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

    public override void BubbleCollision(GameObject other)
    {
        if (hasPopped || other == null) return;
        if (!IsServer && !isLocalFake) return;

        if (other.CompareTag("Player"))
        {
            return;
        }

        if (other.CompareTag("Bubble") && popOnBubbleHit)
        {
            // Ignore collisions between wall segments
            if (other.TryGetComponent<WallBubble>(out _))
            {
                return;
            }

            hitPoints--;
            ApplyDamagedMaterial();

            if (IsServer && TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay)
            {
                ChangeMaterialClientRpc();
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
        {
            base.BubbleMovement();
        }
    }

    public void ApplyDamagedMaterial()
    {
        if (dmgedOutline == null) return;

        MeshRenderer[] renderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in renderers)
        {
            if (renderer == null) continue;

            Material[] materials = renderer.materials;
            if (materials.Length > 1)
            {
                materials[1] = dmgedOutline;
                renderer.materials = materials;
            }
        }
    }

    [ClientRpc]
    private void ChangeMaterialClientRpc()
    {
        ApplyDamagedMaterial();

        if (!IsServer || isLocalFake)
        {
            WallBubble[] allBubbles = FindObjectsByType<WallBubble>(FindObjectsSortMode.None);
            foreach (var bubble in allBubbles)
            {
                if (bubble.isLocalFake && bubble.AssignedSpellID.Value == this.AssignedSpellID.Value)
                {
                    bubble.ApplyDamagedMaterial();
                    break;
                }
            }
        }
    }
}