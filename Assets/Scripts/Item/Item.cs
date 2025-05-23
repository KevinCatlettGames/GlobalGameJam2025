using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private SO_Spell[] spells;

    [Header("Item PickUp")]
    [SerializeField] private GameObject pickUpEffect;
    [SerializeField] private EventReference pickUpEvent;

    [Header("Item Despawn")]
    [SerializeField] private EventReference despawnEvent;
    [SerializeField] private float itemDuration = 10f;
    [SerializeField] private float itemBlinkDuration = 2f;
    [SerializeField] private float itemBlinkIntervall = .4f;
    [SerializeField] private Material itemMaterial;
    public SO_Spell spell;
    private Material spellMaterial;
    [SerializeField] private ParticleSystemRenderer wrapParticleRenderer;
    [SerializeField] private ParticleSystemRenderer sparkleParticleSystem;
    private void Start()
    {
        if (spell == null)
        {
            int r = Random.Range(0, spells.Length);
            spell = spells[r];
        }
        meshFilter.mesh = spell.GetMesh();
        spellMaterial = spell.GetMaterial();
        meshRenderer.material = spellMaterial;
        Material[] effectMaterials = spell.GetEffectMaterials();
        if (effectMaterials != null && effectMaterials.Length == 2)
        {
            wrapParticleRenderer.material = effectMaterials[0];
            sparkleParticleSystem.material = effectMaterials[1];
        }
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
        if (pickUpEffect != null) Instantiate(pickUpEffect, transform.position, Quaternion.identity);
        RuntimeManager.PlayOneShotAttached(pickUpEvent, gameObject);
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
        ItemSpawner.Instance.currentAmount--;    
        RuntimeManager.PlayOneShotAttached(despawnEvent, gameObject);
        Destroy(gameObject);
    }
}