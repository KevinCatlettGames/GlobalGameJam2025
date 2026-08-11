using UnityEngine;
using System.Collections.Generic;
using Unity.Netcode;

public class TutorialMapNetworkInitializer : MonoBehaviour
{
    [SerializeField] List<GameObject> objectsToSpawn;

    private void Start()
    {
        if(LobbyManager.instance && objectsToSpawn.Count > 0)
        {
            foreach(GameObject obj in objectsToSpawn)
            {
                if(!obj.GetComponent<NetworkObject>().IsSpawned)
                    obj.GetComponent<NetworkObject>().Spawn();
            }
        }
    }

    public void DespawnTutorialObjects()
    {
        if (LobbyManager.instance && objectsToSpawn.Count > 0)
        {
            foreach (GameObject obj in objectsToSpawn)
            {
                if (obj.GetComponent<NetworkObject>().IsSpawned)
                    obj.GetComponent<NetworkObject>().Despawn();
            }
        }
    }
}