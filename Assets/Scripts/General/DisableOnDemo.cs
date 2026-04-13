using UnityEngine;

public class DisableOnDemo : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if(LobbyManager.instance != null)
            if(LobbyManager.instance.IsDemo)
                gameObject.SetActive(false);
    }
}