using UnityEngine;

public class ParticleSystemTrigger : MonoBehaviour
{
    [SerializeField] private ParticleSystem particleSystem;

    public void StartParticleSystem()
    {
        particleSystem?.Play();
    }
}
