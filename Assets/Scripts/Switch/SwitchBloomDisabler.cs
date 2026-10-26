using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SwitchBloomDisabler : MonoBehaviour
{
    [SerializeField] private Volume targetVolume;

    private void Awake()
    {
#if UNITY_SWITCH
        // Auto-assign if not set in inspector
        if (targetVolume == null)
            targetVolume = GetComponent<Volume>();

        if (targetVolume != null)
        {
            // Access the runtime profile
            VolumeProfile profile = targetVolume.profile;

            if (profile != null)
            {
                // Find and disable the Bloom component override
                if (profile.TryGet<Bloom>(out Bloom bloom))
                {
                    bloom.active = false;
                    Debug.Log("Bloom successfully disabled for Nintendo Switch.");
                }
            }
        }
#endif
    }
}