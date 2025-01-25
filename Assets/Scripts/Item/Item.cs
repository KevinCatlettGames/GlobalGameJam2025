using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private SO_Spell[] spells;
    private SO_Spell spell;
    private void Start()
    {
        int r = Random.Range(0, spells.Length);
        spell = spells[r];
        meshFilter.mesh = spell.itemMesh;
        meshRenderer.material = spell.itemMaterial;

    }

    public SO_Spell EquipSpell()
    {
        StartCoroutine(DelayedDestroy());
        return spell;
    }

    private IEnumerator DelayedDestroy()
    {
        yield return new WaitForEndOfFrame();
        Destroy(gameObject);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player")) 
        {
            PlayerController player = other.GetComponent<PlayerController>();
            player.UpdateItemToEquip(this, true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            player.UpdateItemToEquip(this, false);
        }
    }
}