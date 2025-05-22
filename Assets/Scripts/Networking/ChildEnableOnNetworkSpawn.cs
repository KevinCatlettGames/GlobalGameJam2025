using UnityEngine; 
using System.Collections;
using Unity.Netcode; 

/// <summary>
/// Makes sure a child is first activated once this NetworkObject is spawned on the server.
/// Used for initialization of effects on children in the correct moment. 
/// </summary>
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