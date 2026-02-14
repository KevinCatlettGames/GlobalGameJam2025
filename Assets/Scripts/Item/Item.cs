using FMODUnity;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Item : NetworkBehaviour
{
    [SerializeField] private MeshFilter meshFilter;
    [SerializeField] private MeshRenderer meshRenderer;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private int spellID = -1;

    [Header("Item PickUp")]
    [SerializeField] private GameObject pickUpEffect;
    [SerializeField] private EventReference pickUpEvent;

    [Header("Item Despawn")]
    [SerializeField] private EventReference despawnEvent;
    [SerializeField] private float itemDuration = 10f;
    [SerializeField] private float itemBlinkDuration = 10f;
    [SerializeField] private float itemBlinkIntervall = 0.4f;
    [SerializeField] private Material itemMaterial;
    [SerializeField] private bool disableDespawn = false;

    private Material spellMaterial;

    [SerializeField] private ParticleSystemRenderer wrapParticleRenderer;
    [SerializeField] private ParticleSystemRenderer sparkleParticleSystem;
    [SerializeField] private ParticleSystemRenderer waveEffect;

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

        spellMaterial = spell.ItemMaterial;
        meshRenderer.material = spellMaterial;
        spriteRenderer.color = spell.ItemEffectColor;

        Material[] effectMaterials = spell.GetEffectMaterials();
        if (effectMaterials != null && effectMaterials.Length >= 3)
        {
            wrapParticleRenderer.material = effectMaterials[0];
            sparkleParticleSystem.material = effectMaterials[1];
            waveEffect.material = effectMaterials[2];
        }
    }

    private IEnumerator DelayedDestroy()
    {
        yield return new WaitForEndOfFrame();

        StopAllCoroutines();
        if(!disableDespawn)
            ItemSpawner.Instance.currentAmount--;
        if (pickUpEffect != null) 
            Instantiate(pickUpEffect, transform.position, Quaternion.identity);
        RuntimeManager.PlayOneShotAttached(pickUpEvent, gameObject);
        if (IsServer && !disableDespawn)
            GetComponent<NetworkObject>().Despawn();
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
}
