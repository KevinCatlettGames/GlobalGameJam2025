using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    public override void InitialiseBubble(int ID, Vector3 dir, Collider playerCollider, int assignedSpellID, bool fakeWithServerSpawn)
    {
        base.InitialiseBubble(ID, dir, playerCollider, assignedSpellID, fakeWithServerSpawn);

        if (playerCollider != null)
            offset = transform.position - playerCollider.transform.position;

        StartCoroutine(EmptyBarrel());
    }

    protected override void BubbleMovement()
    {
        if (!IsServer && !isLocalFake) return;

        if (playerCollider != null)
        {
            transform.position = playerCollider.transform.position + offset;
        }
    }

    private IEnumerator EmptyBarrel()
    {
        float rotation = -(maxAmmo - 1) * 0.5f;

        for (int i = 0; i < maxAmmo; i++)
        {
            Vector3 dir = Quaternion.AngleAxis(spread * rotation, Vector3.up) * direction;
            Vector3 pos = transform.position + direction;

            GameObject bubbleObj = Instantiate(bubblePrefab, pos, Quaternion.LookRotation(dir));

            RevolverBulletBubble bulletScript = bubbleObj.GetComponent<RevolverBulletBubble>();
            if (bulletScript != null)
            {
                bulletScript.RevolverBubble = this;
            }

            BasicBubble bubbleBaseScript = bubbleObj.GetComponent<BasicBubble>();

            if (IsServer)
            {
                NetworkObject netObj = bubbleObj.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    netObj.Spawn();
                }
            }
            else if (isLocalFake)
            {
                // Strip NetworkObject component on local predicted instances
                NetworkObject netObj = bubbleObj.GetComponent<NetworkObject>();
                if (netObj != null)
                {
                    Destroy(netObj);
                }

                if (bubbleBaseScript != null)
                {
                    bubbleBaseScript.isLocalFake = true;
                }
            }

            if (bubbleBaseScript != null)
            {
                int nextSpellID = AssignedSpellID.Value >= 0 ? AssignedSpellID.Value + 1 : -1;
                bubbleBaseScript.InitialiseBubble(OwnerID.Value, dir, playerCollider, nextSpellID, fakeWithServerCaster);
            }

            yield return new WaitForSeconds(delayBetweenShots);
            rotation++;
        }

        yield return new WaitForSeconds(0.1f);

        if (IsServer)
        {
            DisableRevolverMeshClientRpc();
        }

        if (isLocalFake)
        {
            foreach (MeshRenderer renderer in GetComponentsInChildren<MeshRenderer>())
            {
                renderer.enabled = false;
            }
        }

        yield return new WaitForSeconds(3f);

        // Despawn networked object on server or destroy local fake on client
        if (IsServer)
        {
            if (NetworkObject != null && NetworkObject.IsSpawned)
            {
                NetworkObject.Despawn(true);
            }
            else
            {
                Destroy(gameObject);
            }
        }
        else if (isLocalFake)
        {
            Destroy(gameObject);
        }
    }

    [ClientRpc]
    private void DisableRevolverMeshClientRpc()
    {
        if (IsServer || isLocalFake) return;

        if (revolverMesh != null)
        {
            revolverMesh.enabled = false;
        }
    }

    public void AddToHitCount()
    {
        if (isLocalFake) return;

        hitCount++;
        if (hitCount >= maxAmmo)
        {
            CheckAllShotsHitAchievement();
        }
    }

    private void CheckAllShotsHitAchievement()
    {
        if (!IsServer && !isLocalFake) return;

        if ((TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)OwnerID.Value)
            || !AchievementSaveSystem.instance || SceneManager.GetActiveScene().buildIndex == 5 || SceneManager.GetActiveScene().buildIndex == 6) return;

        AchievementSaveSystem achSaveSystem = AchievementSaveSystem.instance;
        achSaveSystem.IncrementStat(19, 1);
        achSaveSystem.IncrementStat(6, 1);
    }
}