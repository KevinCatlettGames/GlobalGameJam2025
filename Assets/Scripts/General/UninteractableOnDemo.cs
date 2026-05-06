using UnityEngine;
using UnityEngine.UI; 

public class UninteractableOnDemo : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (SteamIntegration.instance != null)
            if (!SteamIntegration.instance.IsFullVersion)
                GetComponent<Button>().interactable = false;
    }
}
