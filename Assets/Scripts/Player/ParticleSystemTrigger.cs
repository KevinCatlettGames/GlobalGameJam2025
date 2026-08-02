using UnityEngine;

public class ParticleSystemTrigger : MonoBehaviour
{
    [SerializeField] private ParticleSystem particleSystem;
    public ParticleSystem ParticleSys {get{return particleSystem;} set{particleSystem = value;} 
    }

    public void StartParticleSystem()
    {
        if (particleSystem == null) return;
        particleSystem?.Play();
    }
}