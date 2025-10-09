using UnityEngine; 
using System.Collections;
using Unity.Netcode; 

public class ChildEnableOnNetworkSpawn : NetworkBehaviour
{
    [SerializeField] private GameObject childObject;

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            childObject.SetActive(false);
            StartCoroutine(EnableParticlesAfterDelay());
        }
    }

    private IEnumerator EnableParticlesAfterDelay()
    {
        yield return new WaitForEndOfFrame();
        childObject.SetActive(true);
    }
}