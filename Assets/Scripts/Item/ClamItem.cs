using System.Collections;
using Unity.Netcode; 
using FMODUnity;
using UnityEngine;

public class ClamItem : Item
{
    private void Start()
    {
        if (LobbyManager.instance && !LobbyManager.instance.MapSettings[2].PlayWithMapEvent && IsServer)
        {
            GetComponent<NetworkObject>().Despawn();
            DestroySelfClientRpc();
        }
    }

    protected override IEnumerator DelayedDestroy()
    {
        yield return new WaitForEndOfFrame();
        StopAllCoroutines();
        if (pickUpEffect != null) 
            Instantiate(pickUpEffect, transform.position, Quaternion.identity);
        RuntimeManager.PlayOneShotAttached(pickUpEvent, gameObject);
        gameObject.SetActive(false);
    }

    private void ToggleItem(bool isActive)
    {
        gameObject.SetActive(isActive);
    }

    [ClientRpc]
    private void DestroySelfClientRpc()
    {
        Destroy(gameObject);
    }
}
