using FMODUnity;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class Item : NetworkBehaviour
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
    [SerializeField] private float itemBlinkDuration = 10f;
    [SerializeField] private float itemBlinkIntervall = 0.4f;
    [SerializeField] private Material itemMaterial;

    private Material spellMaterial;
    public SO_Spell spell;

    private NetworkVariable<float> serverSpawnTime = new NetworkVariable<float>(writePerm: NetworkVariableWritePermission.Server);
    private NetworkVariable<int> spellIndex = new NetworkVariable<int>(-1);

    private bool isBlinking = false;
    private float blinkStartTime;

    [SerializeField] private ParticleSystemRenderer wrapParticleRenderer;
    [SerializeField] private ParticleSystemRenderer sparkleParticleSystem;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            int r = 0;
            if (spell == null)
            {
                r = Random.Range(0, spells.Length);
                spell = spells[r];
            }

            spellIndex.Value = r;
            serverSpawnTime.Value = (float)NetworkManager.ServerTime.Time;
            SetupSpell(r);
            StartCoroutine(ServerItemDespawn());
        }
        else
        {
            spellIndex.OnValueChanged += (oldVal, newVal) => SetupSpell(newVal);
            if (spellIndex.Value >= 0)
                SetupSpell(spellIndex.Value);
        }
    }

    public SO_Spell EquipSpell()
    {
        StartCoroutine(DelayedDestroy());
        return spell;
    }


    private void SetupSpell(int index)
    {
        spell = spells[index];
        meshFilter.mesh = spell.GetMesh();

        spellMaterial = spell.GetMaterial();
        meshRenderer.material = spellMaterial;

        Material[] effectMaterials = spell.GetEffectMaterials();
        if (effectMaterials != null && effectMaterials.Length == 2)
        {
            wrapParticleRenderer.material = effectMaterials[0];
            sparkleParticleSystem.material = effectMaterials[1];
        }
    }

    private IEnumerator DelayedDestroy()
    {
        yield return new WaitForEndOfFrame();

        StopAllCoroutines();
        ItemSpawner.Instance.currentAmount--;
        if (pickUpEffect != null) Instantiate(pickUpEffect, transform.position, Quaternion.identity);
        RuntimeManager.PlayOneShotAttached(pickUpEvent, gameObject);
        if (IsServer)
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
