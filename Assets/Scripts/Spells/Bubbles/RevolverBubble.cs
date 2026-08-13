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
        OwnerID.Value = ID;
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

            if (IsServer)
            {
                NetworkObject netObj = bubbleObj.GetComponent<NetworkObject>();
                if (netObj != null)
                    netObj.Spawn();
            }
            else if (isLocalFake)
            {
                Destroy(bubbleObj.GetComponent<NetworkObject>());

                if (bubbleScript != null)
                {
                    bubbleScript.isLocalFake = true;
                }
            }

            if (bubbleScript != null)
            {
                bubbleScript.InitialiseBubble(OwnerID.Value, dir, playerCollider, AssignedSpellID.Value+1, fakeWithServerCaster);
            }

            yield return new WaitForSeconds(delayBetweenShots);
            rotation++;
        }

        yield return new WaitForSeconds(.1f);
        if (IsServer) DisableRevolverMeshClientRpc();
        if(isLocalFake)
            foreach(MeshRenderer meshRenderer in GetComponentsInChildren<MeshRenderer>())
                meshRenderer.enabled = false;
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
       if (!IsServer && !isLocalFake) return;

        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)OwnerID.Value
            || !AchievementSaveSystem.instance || SceneManager.GetActiveScene().buildIndex == 5 || SceneManager.GetActiveScene().buildIndex == 6) return;

        //Debug.Log("Increment all shots hit revolver ach");
        AchievementSaveSystem achSaveSystem = AchievementSaveSystem.instance;
        achSaveSystem.IncrementStat(19, 1);
        achSaveSystem.IncrementStat(6, 1);
    }
}