using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using Unity.Netcode; 

[CreateAssetMenu(fileName = "new Spell", menuName = "Scriptable Objects/SO_Spell/Simple")]
public class SO_Spell : ScriptableObject
{
    [SerializeField] private int spellIndex;
    public int SpellIndex { get { return spellIndex; } }

    [Header("UI")]
    [SerializeField] private Sprite spellIcon;
    [SerializeField] private Sprite usedSpellIcon;
    [SerializeField] private Color indicatorColor;
    public Sprite SpellIcon { get { return spellIcon; } }
    public Sprite UsedSpellIcon { get { return usedSpellIcon;} }
    public Color IndicatorColor {  get { return indicatorColor; } }
    
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
    [SerializeField] protected Color itemEffectColor;
    public Mesh ItemMesh { get { return itemMesh; } }
    public Material ItemMaterial { get { return itemMaterial; } }
    public Color ItemEffectColor { get { return itemEffectColor; } }

    [Header("Sound Events")]
    [SerializeField] protected EventReference castEventStruct;
    [SerializeField] protected EventReference spellEventStruct;
    public EventReference SpellEventStruct { get { return spellEventStruct; } }

    protected BasicBubble bubbleScript;
    virtual public float CastSpell(int ID, Vector3 pos, Vector3 dir, Collider playerCollider)
    {
        dir.Normalize();

        float safeDistance = playerCollider.bounds.extents.z + .5f;
        pos += dir * safeDistance;

        if (NetworkManager.Singleton.IsServer)
        {
            GameObject bubbleInstance = Instantiate(bubble, pos, Quaternion.LookRotation(dir));

            bubbleScript = bubbleInstance.GetComponent<BasicBubble>();
            bubbleScript.InitialiseBubble(ID, bubbleDamage, bubbleKnockback, bubbleSpeed, bubbleRange, bubbleSize, inflationSpeed, dir, castEventStruct, playerCollider);

            bubbleInstance.GetComponent<NetworkObject>().Spawn();
        }

        return spellCooldown;
    }
    public Material[] GetEffectMaterials()
    {
        return effectMaterials;
    }
}
