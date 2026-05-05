using UnityEngine;

public class DisableOnDemo : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(SteamIntegration.instance != null)
            if(!SteamIntegration.instance.IsFullVersion)
                gameObject.SetActive(false);
    }
}