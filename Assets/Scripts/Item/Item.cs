using FMODUnity;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Item : NetworkBehaviour
{
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private int spellID = -1;

    [Header("Item PickUp")]
    [SerializeField] protected GameObject pickUpEffect;
    [SerializeField] protected EventReference pickUpEvent;

    [Header("Item Despawn")]
    [SerializeField] private EventReference despawnEvent;
    [SerializeField] private float itemDuration = 10f;
    [SerializeField] private float itemBlinkDuration = 10f;
    [SerializeField] private float itemBlinkIntervall = 0.4f;
    [SerializeField] private Material itemMaterial;
    [SerializeField] private bool disableDespawn = false;
    [SerializeField] private float disableTime = .5f;
    [SerializeField] private List<GameObject> visuals;
    
    private Material spellMaterial;

    [SerializeField] private ParticleSystemRenderer wrapParticleRenderer;
    [SerializeField] private ParticleSystemRenderer sparkleParticleSystem;
    [SerializeField] private ParticleSystemRenderer waveEffect;

    public Action OnCollected;

    public override void OnNetworkSpawn()
    {
        if (IsServer && !disableDespawn)
        {
            StartCoroutine(ServerItemDespawn());
        }
        if (spellID != -1)
            SetupSpellClientRpc(spellID);
    }

    public int EquipSpell()
    {
        StartCoroutine(DelayedDestroy());
        return spellID;
    }

    [ClientRpc]
    public void SetupSpellClientRpc(int index)
    {
        spellID = index;
        SO_Spell spell = ItemSpawner.Instance.GetSpellByIndex(spellID);
        meshFilter.mesh = spell.ItemMesh;
        spellMaterial = spell.ItemMaterials[0];
        meshRenderer.materials = spell.ItemMaterials;
        Material outlineMaterial = meshRenderer.materials[1];
        
        Color[] itemEffectColors = spell.EffectColors;
        if (itemEffectColors != null)
        {
            spriteRenderer.color = itemEffectColors[0];
            wrapParticleRenderer.material.SetColor("_TopColor", itemEffectColors[1]);
            sparkleParticleSystem.material.SetColor("_TopColor", itemEffectColors[2]);
            waveEffect.material.SetColor("_TopColor", itemEffectColors[3]);
            outlineMaterial.SetColor("_OutlineColor", itemEffectColors[4]);
        }
    }

    protected virtual IEnumerator DelayedDestroy()
    {
        yield return new WaitForEndOfFrame();

        if (!disableDespawn)
        {
            StopAllCoroutines();
            ItemSpawner.Instance.currentAmount--;
        }
        else
        {
            OnCollected?.Invoke();
        }
        if (pickUpEffect != null) 
            Instantiate(pickUpEffect, transform.position, Quaternion.identity);
        PlayPickupSoundServerRpc();
        if (!disableDespawn)
        {
            if (IsServer)
                GetComponent<NetworkObject>().Despawn();
        }
        else
        {
            foreach (GameObject visual in visuals)
            {
                visual.SetActive(false);
            }
            SphereCollider sphereCollider = GetComponent<SphereCollider>();
            sphereCollider.enabled = false;
            yield return new WaitForSeconds(disableTime);
            foreach (GameObject visual in visuals)
            {
                visual.SetActive(true);
            }
            sphereCollider.enabled = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer)
            return;
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance.PlayingLocal)
            {
                UpdateItemToEquipLocal(other.gameObject, this, true);
            }
            else
            {

                var playerNetworkObject = other.GetComponent<NetworkObject>();
                if (playerNetworkObject != null)
                {
                    UpdateItemToEquipServerRpc(playerNetworkObject, GetComponent<NetworkObject>(), true);
                }
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer)
            return;
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance.PlayingLocal)
            {
                UpdateItemToEquipLocal(other.gameObject, this, false);
            }
            else
            {
                var playerNetworkObject = other.GetComponent<NetworkObject>();
                if (playerNetworkObject != null)
                {
                    UpdateItemToEquipServerRpc(playerNetworkObject, GetComponent<NetworkObject>(), false);
                }
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void UpdateItemToEquipServerRpc(NetworkObjectReference playerRef, NetworkObjectReference itemRef, bool isInRange)
    {
        if (playerRef.TryGet(out NetworkObject playerNetObj) &&
            itemRef.TryGet(out NetworkObject itemNetObj))
        {
            var player = playerNetObj.GetComponent<PlayerController>();
            var item = itemNetObj.GetComponent<Item>();

            if (player != null && item != null)
            {
                player.UpdateItemToEquip(item, isInRange);
            }
        }
    }

    private void UpdateItemToEquipLocal(GameObject player, Item item, bool isInRange)
    {
        if (player != null && item != null)
            player.GetComponent<PlayerController>().UpdateItemToEquip(item, isInRange);
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartBlinkEffectServerRpc() => StartBlinkEffectClientRpc();

    [ClientRpc]
    private void StartBlinkEffectClientRpc()
    {
        StartCoroutine(ClientBlinkEffectLoop());
    }
    private IEnumerator ClientBlinkEffectLoop()
    {
        bool toggle = true;

        while (true)
        {
            meshRenderer.material = toggle ? itemMaterial : spellMaterial;
            toggle = !toggle;
            yield return new WaitForSeconds(itemBlinkIntervall);
        }
    }

    private IEnumerator ServerItemDespawn()
    {
        yield return new WaitForSeconds(itemDuration);

        StartBlinkEffectServerRpc();

        yield return new WaitForSeconds(itemBlinkDuration);

        ItemSpawner.Instance.currentAmount--;
        GetComponent<NetworkObject>().Despawn();
    }

    [ServerRpc(RequireOwnership = false)]
    private void PlayPickupSoundServerRpc()
    {
        PlayPickupSoundClientRpc();
    }

    [ClientRpc]
    private void PlayPickupSoundClientRpc()
    {
        RuntimeManager.PlayOneShotAttached(pickUpEvent, gameObject);

    }
}
