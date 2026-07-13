using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "new Spell", menuName = "Scriptable Objects/SO_Spell/Rapid")]
public class SO_Rapid : SO_Spell
{
    // --- FIX: UPDATED METHOD SIGNATURE TO MATCH PARENT (int assignedCastID) ---
    public override float CastSpell(int ID, Vector3 pos, Vector3 dir, Collider playerCollider, bool isUlt, ulong senderClientId, int assignedCastID)
    {
        dir.Normalize();

        // 1. Calculate the base local spawn positions for both bubbles
        float safeDistance1 = playerCollider.bounds.extents.z + 1f;
        Vector3 baseSpawnPos1 = pos + (dir * safeDistance1);

        float safeDistance2 = safeDistance1 - 2.5f;
        Vector3 baseSpawnPos2 = pos + (dir * safeDistance2);

        // --- FIX: GENERATE SEQUENTIAL INTEGER IDs ---
        int castID1 = assignedCastID;
        int castID2 = assignedCastID + 1;

        // ==========================================
        // 1. SERVER SIDE: Fast-Forward and Spawn Real Bubbles
        // ==========================================
        if (NetworkManager.Singleton.IsServer)
        {
            // Calculate Latency (One-Way Trip Time in seconds)
            float rttInMs = NetworkManager.Singleton.NetworkConfig.NetworkTransport.GetCurrentRtt(senderClientId);
            float oneWayTime = (rttInMs / 2f) / 1000f;
            oneWayTime += Time.fixedDeltaTime / 2f; // Account for tick alignment

            // First Server Bubble
            GameObject bubbleInstance1 = Instantiate(bubble, baseSpawnPos1, Quaternion.LookRotation(dir));
            BasicBubble script1 = bubbleInstance1.GetComponent<BasicBubble>();
            script1.InitialiseBubble(ID, dir, playerCollider);
            script1.castID = castID1; // Assign Int ID 1

            // Fast-forward first bubble
            bubbleInstance1.transform.position += dir * (script1.Speed * oneWayTime);
            bubbleInstance1.GetComponent<NetworkObject>().Spawn();

            // Second Server Bubble
            GameObject bubbleInstance2 = Instantiate(bubble, baseSpawnPos2, Quaternion.LookRotation(dir));
            BasicBubble script2 = bubbleInstance2.GetComponent<BasicBubble>();
            script2.InitialiseBubble(ID, dir, playerCollider);
            script2.castID = castID2; // Assign Int ID 2

            // Fast-forward second bubble
            bubbleInstance2.transform.position += dir * (script2.Speed * oneWayTime);
            bubbleInstance2.GetComponent<NetworkObject>().Spawn();

            // Hide both real server bubbles from the casting player
            if (!GameManager.Instance.PlayingLocal)
            {
                if (bubbleInstance1.TryGetComponent<NetworkObject>(out var netObj1)) netObj1.NetworkHide(senderClientId);
                if (bubbleInstance2.TryGetComponent<NetworkObject>(out var netObj2)) netObj2.NetworkHide(senderClientId);
            }
        }

        // --- LOCAL CASTING CLIENT SIDE ---
        if (!NetworkManager.Singleton.IsServer && NetworkManager.Singleton.LocalClientId == senderClientId)
        {
            PlayerController playerCtrl = playerCollider.GetComponent<PlayerController>();

            // --- Local Fake Bubble 1 ---
            GameObject fakeInstance1 = Instantiate(fakeBubble != null ? fakeBubble : bubble, baseSpawnPos1, Quaternion.LookRotation(dir));
            if (fakeInstance1.TryGetComponent<NetworkObject>(out var netObj1)) Destroy(netObj1);

            fakeInstance1.layer = LayerMask.NameToLayer("FakeProjectiles");

            BasicBubble fakeScript1 = fakeInstance1.GetComponent<BasicBubble>();
            if (fakeScript1 != null)
            {
                fakeScript1.isLocalFake = true;
                fakeScript1.castID = castID1; // Assign Int ID 1
                fakeScript1.InitialiseBubble(ID, dir, playerCollider);
            }

            if (playerCtrl != null && fakeScript1 != null) playerCtrl.RegisterLocalFake(fakeScript1);

            // --- Local Fake Bubble 2 ---
            GameObject fakeInstance2 = Instantiate(fakeBubble != null ? fakeBubble : bubble, baseSpawnPos2, Quaternion.LookRotation(dir));
            if (fakeInstance2.TryGetComponent<NetworkObject>(out var netObj2)) Destroy(netObj2);

            fakeInstance2.layer = LayerMask.NameToLayer("FakeProjectiles");

            BasicBubble fakeScript2 = fakeInstance2.GetComponent<BasicBubble>();
            if (fakeScript2 != null)
            {
                fakeScript2.isLocalFake = true;
                fakeScript2.castID = castID2; // Assign Int ID 2
                fakeScript2.InitialiseBubble(ID, dir, playerCollider);
            }

            if (playerCtrl != null && fakeScript2 != null) playerCtrl.RegisterLocalFake(fakeScript2);
        }

        return spellCooldown;
    }
}