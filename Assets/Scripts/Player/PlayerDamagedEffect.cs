using UnityEngine;

public class PlayerDamagedEffect : MonoBehaviour
{

    private ParticleSystem.MainModule main;
    private ParticleSystem.EmissionModule emission;
    [SerializeField] private float emissionMod = .1f;
    [SerializeField] private float speedMod = .1f;
    [SerializeField] private float distance = 1f;
    void Start()
    {
        ParticleSystem damagedParticleSystem = GetComponent<ParticleSystem>();
        main = damagedParticleSystem.main;
        emission = damagedParticleSystem.emission;
    }


    public void UpdateParticleSystem(float damage)
    {

        if (damage < 0)
        {
            emission.rateOverTime = 0;
            main.startSpeed = 0;
        }
        else
        {
            emission.rateOverTime = damage * emissionMod;
            main.startSpeed = damage * speedMod;
            main.startLifetime = distance / (damage * speedMod);
        }
    }
}
