using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private SO_Spell[] spells;

    [Header ("Item Despawn")]
    [SerializeField] private GameObject pickUpEffect;
    [SerializeField] private float itemDuration = 10f;
    [SerializeField] private float itemBlinkDuration = 2f;
    [SerializeField] private float itemBlinkIntervall = .4f;
    [SerializeField] private Material itemMaterial;
    public SO_Spell spell;
    private Material spellMaterial;
    private void Start()
    {
        int r = Random.Range(0, spells.Length);
        spell = spells[r];
        meshFilter.mesh = spell.GetMesh();
        spellMaterial = spell.GetMaterial();
        meshRenderer.material = spellMaterial;
        StartCoroutine(ItemDespawn());
    }

    public SO_Spell EquipSpell()
    {
        StartCoroutine(DelayedDestroy());
        return spell;
    }

    private IEnumerator DelayedDestroy()
    {
        yield return new WaitForEndOfFrame();
        ItemSpawner.Instance.currentAmount--;
        if(pickUpEffect != null) Instantiate(pickUpEffect);
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

    private IEnumerator ItemDespawn()
    {
        yield return new WaitForSeconds(itemDuration);
        bool material_b = true;
        float duration = itemBlinkDuration;
        while (duration > 0) 
        {
            if (material_b)
            {
                meshRenderer.material = itemMaterial;
            }
            else
            {
                meshRenderer.material = spellMaterial;
            }
            material_b = !material_b;
            yield return new WaitForSeconds(itemBlinkIntervall);
            duration -= itemBlinkIntervall;
        }
    }
}