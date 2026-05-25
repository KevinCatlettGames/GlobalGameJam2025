using UnityEngine;

public class DisableOnDemo : MonoBehaviour
{
    public bool destroyInstead = false; 

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(SteamIntegration.instance != null)
            if(!SteamIntegration.instance.IsFullVersion)
                if(destroyInstead)
                    Destroy(gameObject);
                else
                    gameObject.SetActive(false);
    }
}