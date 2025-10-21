using Unity.Netcode;
using UnityEngine;

[CreateAssetMenu(fileName = "new Spell", menuName = "Scriptable Objects/SO_Spell/Rapid")]
public class SO_Rapid : SO_Spell
{
    public override float CastSpell(int ID, Vector3 pos, Vector3 dir, Collider playerCollider)
    {
        dir.Normalize();

        float safeDistance = playerCollider.bounds.extents.z + 1f;
        Vector3 spawnPos = pos + (dir * safeDistance);

        if (NetworkManager.Singleton.IsServer)
        {
            GameObject bubbleInstance = Instantiate(bubble, spawnPos, Quaternion.LookRotation(dir));

            bubbleScript = bubbleInstance.GetComponent<BasicBubble>();
            bubbleScript.InitialiseBubble(ID, bubbleDamage, bubbleKnockback, bubbleSpeed, bubbleRange, bubbleSize, inflationSpeed, dir, castEventStruct, playerCollider);

            bubbleInstance.GetComponent<NetworkObject>().Spawn();
            
            safeDistance -= 2.5f;
            spawnPos = pos + (dir * safeDistance);

            bubbleInstance = Instantiate(bubble, spawnPos, Quaternion.LookRotation(dir));

            bubbleScript = bubbleInstance.GetComponent<BasicBubble>();
            bubbleScript.InitialiseBubble(ID, bubbleDamage, bubbleKnockback, bubbleSpeed, bubbleRange, bubbleSize, inflationSpeed, dir, castEventStruct, playerCollider);

            bubbleInstance.GetComponent<NetworkObject>().Spawn();
        }

        return spellCooldown;
    }
}
