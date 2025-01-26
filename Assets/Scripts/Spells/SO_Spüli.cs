using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SpüliSpell", menuName = "ScriptableObject/Spell/Spüli")]
public class SO_Spüli : SO_Spell
{
    [SerializeField] private Material[] materials;

    public override Material GetMaterial()
    {
        int r =  Random.Range(0, materials.Length);
        return materials[r];
    }
}
