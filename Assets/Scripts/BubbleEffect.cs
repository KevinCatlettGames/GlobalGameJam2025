using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BubbleEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem popParticleSystem;
    [SerializeField] private float sizeBurstRatio = 25f;

    public void Initialise(float size)
    {
        if (popParticleSystem == null) return;
        ParticleSystem.Burst burst = new ParticleSystem.Burst();
        burst.count = size * sizeBurstRatio;
        popParticleSystem.emission.SetBurst(0, burst);
        popParticleSystem.Play();
    }
}
