using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

[CreateAssetMenu(fileName = "new Spell", menuName = "ScriptableObject/Spell/Simple")]
public class SO_Spell : ScriptableObject
{
    [Header("Bubble")]
    [SerializeField] protected GameObject bubble;
    [SerializeField] protected float bubbleDamage = 1.0f;
    [SerializeField] protected float bubbleKockback = 1.0f;
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

    protected BasicBubble bubbleScript;
    public virtual float CastSpell(Vector3 pos, Vector3 dir)
    {
        dir.Normalize();
        pos += dir * (bubbleSize / 2 + 1);
        bubbleScript = Instantiate(bubble, pos, Quaternion.LookRotation(dir)).GetComponent<BasicBubble>();
        bubbleScript.InitialiseBubble(bubbleDamage, bubbleKockback, bubbleSpeed, bubbleRange, bubbleSize, dir);
        RuntimeManager.PlayOneShotAttached(castEventStruct, bubbleScript.gameObject);
        return spellCooldown;
    }
    public Mesh GetMesh()
    {
        return itemMesh;
    }
    public Material GetMaterial() 
    {
        return itemMaterial;
    }
}
