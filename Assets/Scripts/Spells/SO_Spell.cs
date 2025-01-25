using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    [SerializeField] public Mesh itemMesh;
    [SerializeField] public Material itemMaterial;

    protected BasicBubble bubbleScript;
    public virtual float CastSpell(Vector3 position, Vector3 direction)
    {
        direction.Normalize();
        position += direction * (bubbleSize / 2 + 1);
        bubbleScript = Instantiate(bubble, position, Quaternion.LookRotation(direction)).GetComponent<BasicBubble>();
        bubbleScript.InitialiseBubble(bubbleDamage, bubbleKockback, bubbleSpeed, bubbleRange, bubbleSize, direction);
        return spellCooldown;
    }
}
