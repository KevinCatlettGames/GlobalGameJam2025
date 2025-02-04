using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

[CreateAssetMenu(fileName = "new Spell", menuName = "ScriptableObject/Spell/Simple")]
public class SO_Spell : ScriptableObject
{
    [Header("UI")]
    public Sprite SpellIcon;

    [Header("Bubble")]
    [SerializeField] protected GameObject bubble;
    [SerializeField] protected float bubbleDamage = 1.0f;
    [SerializeField] protected float bubbleKnockback = 1.0f;
    [SerializeField] protected float bubbleSpeed = 1.0f;
    [SerializeField] protected float bubbleRange = 1.0f;
    [SerializeField] protected float bubbleSize = 1.0f;

    [Header("Spell")]
    [SerializeField] protected float spellCooldown = 1.0f;

    [Header("Pickup")]
    [SerializeField] protected Mesh itemMesh;
    [SerializeField] protected Material itemMaterial;

    [Header("Sound Events")]
    [SerializeField] protected EventReference castEventStruct;
    [SerializeField] protected EventReference spellEventStruct;

    protected BasicBubble bubbleScript;
    public virtual float CastSpell(Vector3 pos, Vector3 dir, Collider playerCollider)
    {
        dir.Normalize();
        pos += dir * (bubbleSize / 2f + 1f);
        bubbleScript = Instantiate(bubble, pos, Quaternion.LookRotation(dir)).GetComponent<BasicBubble>();
        bubbleScript.InitialiseBubble(bubbleDamage, bubbleKnockback, bubbleSpeed, bubbleRange, bubbleSize, dir, castEventStruct, playerCollider);
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
}
