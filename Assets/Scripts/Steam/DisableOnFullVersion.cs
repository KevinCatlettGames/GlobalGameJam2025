using UnityEngine;

public class DisableOnFullVersion : MonoBehaviour
{
    public bool destroyInstead = false;
    void Start()
    {
        if (SteamIntegration.instance != null)
            if (SteamIntegration.instance.IsFullVersion)
                if (destroyInstead)
                    Destroy(gameObject);
                else
                    gameObject.SetActive(false);
    }
}