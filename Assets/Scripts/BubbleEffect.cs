using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BubbleEffect : MonoBehaviour
{
    [SerializeField] private ParticleSystem particleSystem;
    [SerializeField] private float sizeBurstRatio = 25f;

    public void Initialise(float size)
    {
        ParticleSystem.Burst burst = new ParticleSystem.Burst();
        burst.count = size * sizeBurstRatio;
        particleSystem.emission.SetBurst(0, burst);
        particleSystem.Play();
    }
}
