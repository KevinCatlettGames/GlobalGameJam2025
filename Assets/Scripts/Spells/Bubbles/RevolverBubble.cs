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

        if (playerCollider != null)
            offset = transform.position - playerCollider.transform.position;
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

            if (IsServer)
            {
                if (bubbleScript != null)
                {
                    bubbleScript.syncedCastID.Value = uniqueBulletID;
                    bubbleScript.castID = uniqueBulletID;
                }

                NetworkObject netObj = bubbleObj.GetComponent<NetworkObject>();
                if (netObj != null)
                    netObj.Spawn();
            }
            else if (isLocalFake)
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

            if (bubbleScript != null)
            {
                bubbleScript.InitialiseBubble(OwnerID, dir, playerCollider);
            }

            yield return new WaitForSeconds(delayBetweenShots);
            rotation++;
        }

        yield return new WaitForSeconds(.1f);
        if(isLocalFake && visualChildMesh) visualChildMesh.GetComponent<MeshRenderer>().enabled = false;
        if (IsServer) DisableRevolverMeshClientRpc();
        yield return new WaitForSeconds(3f);
        Destroy(gameObject);
    }

    [ClientRpc]
    void DisableRevolverMeshClientRpc()
    {
        if(revolverMesh)
            revolverMesh.enabled = false;
    }

    public void AddToHitCount()
    {
        if (isLocalFake) return;

        hitCount++;
        if (hitCount >= maxAmmo)
            CheckAllShotsHitAchievement();
    }

    private void CheckAllShotsHitAchievement()
    {
        if (!IsServer) return;

        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)OwnerID
            || !SteamIntegration.instance) return;

        SteamIntegration steamIntegration = SteamIntegration.instance;
        steamIntegration.IncrementIntSteamStat(steamIntegration.allShotsHitStatID, 1, steamIntegration.StatThresholds[steamIntegration.allShotsHitStatID], steamIntegration.allRevolverShotsHitAchievementID);
    }
}