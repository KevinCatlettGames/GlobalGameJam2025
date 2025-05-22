using UnityEngine; 
using System.Collections;
using Unity.Netcode; 

public class ParticleSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject childObject; // Optional: for enabling/disabling

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            // Optional: hide if visual flicker is too visible
            childObject.SetActive(false);

            // Delay to wait for transform sync
            StartCoroutine(EnableParticlesAfterDelay());
        }
    }

    private IEnumerator EnableParticlesAfterDelay()
    {
        yield return new WaitForEndOfFrame(); // or WaitForSeconds(0.05f)
        
        childObject.SetActive(true); // if you disabled it
    }
}