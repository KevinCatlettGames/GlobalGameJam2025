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
        offset = transform.position - playerCollider.transform.position;
        this.playerCollider = playerCollider;

        StartCoroutine(EmptyBarrel());
    }

    protected override void BubbleMovement()
    {
        transform.position = playerCollider.transform.position + offset;
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
            bubbleObj.GetComponent<RevolverBulletBubble>().RevolverBubble = this; 
            
            NetworkObject netObj = bubbleObj.GetComponent<NetworkObject>();
            if (netObj != null)
                netObj.Spawn();

            BasicBubble bubbleScript = bubbleObj.GetComponent<BasicBubble>();
            bubbleScript.InitialiseBubble(OwnerID, dir, playerCollider);

            yield return new WaitForSeconds(delayBetweenShots);
            rotation++;
            
        }
        yield return new WaitForSeconds(.1f);
        revolverMesh.enabled = false;
        yield return new WaitForSeconds(3f);
        Destroy(gameObject);
    }

    public void AddToHitCount()
    {
        hitCount++;
        if (hitCount >= maxAmmo)
        {
            CheckAllShotsHitAchievement();
        }
    }

    private void CheckAllShotsHitAchievement()
    {
        if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay && NetworkManager.Singleton.LocalClientId != (ulong)OwnerID 
            || !SteamIntegration.instance) return;
        
        SteamIntegration steamIntegration = SteamIntegration.instance;
        steamIntegration.IncrementIntSteamStat(steamIntegration.allShotsHitStatID, 1, steamIntegration.StatThresholds[steamIntegration.allShotsHitStatID], steamIntegration.allRevolverShotsHitAchievementID);
    }
}