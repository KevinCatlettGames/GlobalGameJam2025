using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class BubbleEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem popParticleSystem;
    [SerializeField] private float sizeBurstRatio = 25f;

    public void Initialise(float size)
    {
        if (popParticleSystem == null) return; 
        SpawnPopEffect(size);
    }
    
    private void SpawnPopEffect(float size)
    {
        // This ensures that the effect is played for all clients
        ParticleSystem.Burst burst = new ParticleSystem.Burst();
        burst.count = size * sizeBurstRatio;
        popParticleSystem.emission.SetBurst(0, burst);
        popParticleSystem.Play();
    }
}