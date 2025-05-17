using System.Collections;
using Unity.Netcode;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Item : NetworkBehaviour
{
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private SO_Spell[] spells;

    [Header("Item Despawn")]
    [SerializeField] private GameObject pickUpEffect;
    [SerializeField] private float itemDuration = 10f;
    [SerializeField] private float itemBlinkDuration = 10f;
    [SerializeField] private float itemBlinkIntervall = 0.4f;
    [SerializeField] private Material itemMaterial;

    private Material spellMaterial;
    [SerializeField] private ParticleSystemRenderer wrapParticleRenderer;
    [SerializeField] private ParticleSystemRenderer sparkleParticleSystem;
    private NetworkVariable<float> serverSpawnTime = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Server);
    private bool isBlinking = false;
    private float blinkStartTime;
    
    // Network synced spell index
    private NetworkVariable<int> spellIndex = new NetworkVariable<int>(-1);
    
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            int r = Random.Range(0, spells.Length);
            spellIndex.Value = r;
            serverSpawnTime.Value = (float)NetworkManager.ServerTime.Time;
            SetupSpell(r);
            StartCoroutine(ServerItemDespawn());
        }
        else
        {
            spellIndex.OnValueChanged += (oldVal, newVal) => SetupSpell(newVal);
            if (spellIndex.Value >= 0) SetupSpell(spellIndex.Value);
        }
    }
    
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
        if (IsServer)
        {
            StartCoroutine(DelayedDestroyServer());
        }
        return spell;
    }

    private IEnumerator DelayedDestroyServer()
    {
        yield return new WaitForEndOfFrame();
        ItemSpawner.Instance.currentAmount--;

        if (pickUpEffect != null)
        {
            GameObject effect = Instantiate(pickUpEffect, transform.position, Quaternion.identity);
            NetworkObject netObj = effect.GetComponent<NetworkObject>();
            netObj.Spawn(true); // Optional: pass ownership
        }

        GetComponent<NetworkObject>().Despawn();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            var playerNetworkObject = other.GetComponent<NetworkObject>();
            if (playerNetworkObject != null)
            {
                UpdateItemToEquipServerRpc(playerNetworkObject, GetComponent<NetworkObject>(), true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;

        if (other.CompareTag("Player"))
        {
            var playerNetworkObject = other.GetComponent<NetworkObject>();
            if (playerNetworkObject != null)
            {
                UpdateItemToEquipServerRpc(playerNetworkObject, GetComponent<NetworkObject>(), false);
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

        ItemSpawner.Instance.currentAmount--;
        GetComponent<NetworkObject>().Despawn();
    }

    private IEnumerator ItemDespawn()
    {
        yield return new WaitForSeconds(itemDuration);

        float duration = itemBlinkDuration;
        bool toggle = true;
        while (duration > 0)
        {
            meshRenderer.material = toggle ? itemMaterial : spellMaterial;
            toggle = !toggle;
            yield return new WaitForSeconds(itemBlinkIntervall);
            duration -= itemBlinkIntervall;
        }

        ItemSpawner.Instance.currentAmount--;
        GetComponent<NetworkObject>().Despawn();
    }
}
