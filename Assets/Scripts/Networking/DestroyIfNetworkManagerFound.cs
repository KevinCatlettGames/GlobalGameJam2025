using Unity.Netcode;
using UnityEngine;

public class DestroyIfNetworkManagerFound : MonoBehaviour
{
    private void Awake()
    {
        if(NetworkManager.Singleton && NetworkManager.Singleton.gameObject != this.gameObject)
            Destroy(gameObject);
    }
}
