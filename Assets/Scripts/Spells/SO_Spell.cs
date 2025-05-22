using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using Unity.Netcode; 

[CreateAssetMenu(fileName = "new Spell", menuName = "ScriptableObject/Spell/Simple")]
public class SO_Spell : ScriptableObject
{

    public int spellIndex; 
    
    [Header("UI")]
    public Sprite SpellIcon;

    [Header("Bubble")]
    [SerializeField] protected GameObject bubble;
    [SerializeField] protected float bubbleDamage = 1.0f;
    [SerializeField] protected float bubbleKnockback = 1.0f;
    [SerializeField] protected float bubbleSpeed = 1.0f;
    [SerializeField] protected float bubbleRange = 1.0f;
    [SerializeField] protected float bubbleSize = 1.0f;
    [SerializeField] protected float inflationSpeed = 1.0f;

    [Header("Spell")]
    [SerializeField] protected float spellCooldown = 1.0f;

    [Header("Pickup")]
    [SerializeField] protected Mesh itemMesh;
    [SerializeField] protected Material itemMaterial;
    [SerializeField] protected Material[] effectMaterials;

    [Header("Sound Events")]
    [SerializeField] protected EventReference castEventStruct;
    [SerializeField] protected EventReference spellEventStruct;

    protected BasicBubble bubbleScript;
    public float CastSpell(int ID, Vector3 pos, Vector3 dir, Collider playerCollider)
    {
        dir.Normalize();

        // // Calculate safe distance: player collider half depth + spell bubble radius + margin
        float safeDistance = playerCollider.bounds.extents.z + (bubbleSize / 2f) + 0.2f;
        pos += dir * safeDistance;

        if (NetworkManager.Singleton.IsServer)
        {
            GameObject bubbleInstance = Instantiate(bubble, pos, Quaternion.LookRotation(dir));

            bubbleScript = bubbleInstance.GetComponent<BasicBubble>();
            bubbleScript.InitialiseBubble(ID, bubbleDamage, bubbleKnockback, bubbleSpeed, bubbleRange, bubbleSize,
                inflationSpeed, dir, castEventStruct, playerCollider);

            bubbleInstance.GetComponent<NetworkObject>().Spawn();
        }

        return spellCooldown;
    }
    public Mesh GetMesh()
    {
        return itemMesh;
    }
    public virtual Material GetMaterial() 
    {
        return itemMaterial;
    }
    public EventReference GetSpellEventStruct() 
    {
        return spellEventStruct;
    }
    public Material[] GetEffectMaterials()
    {
        return effectMaterials;
    }
}
