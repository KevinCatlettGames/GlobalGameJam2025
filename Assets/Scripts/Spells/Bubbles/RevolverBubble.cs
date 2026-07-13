using FMODUnity;
using System.Collections;
using UnityEngine;
using Unity.Netcode;

public class RevolverBubble : BasicBubble
{
    [Header("Special Stats")]
    [SerializeField] private int maxAmmo = 6;
    [SerializeField] private float delayBetweenShots = 0.02f;
    [SerializeField] private float spread = 2f;
    [SerializeField] private GameObject bubblePrefab;
    [SerializeField] private MeshRenderer revolverMesh;

    private int hitCount = 0;
    private Vector3 offset;

    public override void InitialiseBubble(int ID, Vector3 dir, Collider playerCollider)
    {
        OwnerID = ID;
        direction = dir;

        // Safety: ensure playerCollider isn't null before computing offset
        if (playerCollider != null)
        {
            offset = transform.position - playerCollider.transform.position;
        }
        this.playerCollider = playerCollider;

        StartCoroutine(EmptyBarrel());
    }

    protected override void BubbleMovement()
    {
        if (playerCollider != null)
        {
            transform.position = playerCollider.transform.position + offset;
        }
    }

    private IEnumerator EmptyBarrel()
    {
        Vector3 pos;
        float rotation = -(maxAmmo - 1);
        rotation *= .5f;

        // Cache the base castID to protect against parent initialization delays
        int baseCastID = this.castID;

        for (int i = 0; i < maxAmmo; i++)
        {
            Vector3 dir = Quaternion.AngleAxis(spread * rotation, Vector3.up) * direction;
            pos = transform.position + direction;
            GameObject bubbleObj = Instantiate(bubblePrefab, pos, Quaternion.LookRotation(dir));

            var bulletScript = bubbleObj.GetComponent<RevolverBulletBubble>();
            if (bulletScript != null)
            {
                bulletScript.RevolverBubble = this;
            }

            BasicBubble bubbleScript = bubbleObj.GetComponent<BasicBubble>();
            int uniqueBulletID = (baseCastID * 10) + i;

            // --- CLIENT / SERVER SPAWN GATE ---
            if (isLocalFake)
            {
                Destroy(bubbleObj.GetComponent<NetworkObject>());
                bubbleObj.layer = LayerMask.NameToLayer("FakeProjectiles");

                if (bubbleScript != null)
                {
                    bubbleScript.isLocalFake = true;
                    bubbleScript.castID = uniqueBulletID;

                    var playerCtrl = playerCollider?.GetComponent<PlayerController>();
                    if (playerCtrl != null) playerCtrl.RegisterLocalFake(bubbleScript);
                }
            }
            else if (IsServer)
            {
                if (bubbleScript != null)
                {
                    // CRITICAL FIX: Assign the NetworkVariable *BEFORE* Spawning the object.
                    // In Netcode for GameObjects, modifying a NetworkVariable right before calling Spawn() 
                    // ensures the payload is baked into the initial spawn payload packet. 
                    bubbleScript.syncedCastID.Value = uniqueBulletID;
                    bubbleScript.castID = uniqueBulletID;
                }

                NetworkObject netObj = bubbleObj.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    netObj.Spawn();
                }
            }

            if (bubbleScript != null)
            {
                bubbleScript.InitialiseBubble(OwnerID, dir, playerCollider);
            }

            yield return new WaitForSeconds(delayBetweenShots);
            rotation++;
        }

        yield return new WaitForSeconds(.1f);
        if (revolverMesh != null) revolverMesh.enabled = false;
        yield return new WaitForSeconds(3f);
        Destroy(gameObject);
    }

    public void AddToHitCount()
    {
        // Fakes can track hit count locally if desired, but achievements stay server-authoritative
        if (isLocalFake) return;

        hitCount++;
        if (hitCount >= maxAmmo)
        {
            CheckAllShotsHitAchievement();
        }
    }

    private void CheckAllShotsHitAchievement()
    {
        if (!IsServer) return; // Strict safety check

        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)OwnerID
            || !SteamIntegration.instance) return;

        SteamIntegration steamIntegration = SteamIntegration.instance;
        steamIntegration.IncrementIntSteamStat(steamIntegration.allShotsHitStatID, 1, steamIntegration.StatThresholds[steamIntegration.allShotsHitStatID], steamIntegration.allRevolverShotsHitAchievementID);
    }
}