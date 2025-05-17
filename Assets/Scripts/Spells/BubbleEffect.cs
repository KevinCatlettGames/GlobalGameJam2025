using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BubbleEffect : NetworkBehaviour
{
    [SerializeField] private ParticleSystem popParticleSystem;
    [SerializeField] private float sizeBurstRatio = 25f;

    public void Initialise(float size)
    {
        if (popParticleSystem == null) return;
        
        // Trigger on the client where the effect should appear
        if (IsServer)
        {
            // Server sends a ClientRpc to spawn the effect on all clients
            SpawnPopEffectClientRpc(size);
        }
    }

    // ClientRpc to ensure the effect is spawned on clients
    [ClientRpc]
    private void SpawnPopEffectClientRpc(float size)
    {
        // This ensures that the effect is played for all clients
        ParticleSystem.Burst burst = new ParticleSystem.Burst();
        burst.count = size * sizeBurstRatio;
        popParticleSystem.emission.SetBurst(0, burst);
        popParticleSystem.Play();
    }
}