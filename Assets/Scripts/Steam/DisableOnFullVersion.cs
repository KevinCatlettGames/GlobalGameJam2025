using UnityEngine;

public class DisableOnFullVersion : MonoBehaviour
{
    public bool destroyInstead = false;
    public float delay = .5f;
    void OnEnable()
    {
        Invoke(nameof(Evaluate), delay);
    }

    void Evaluate()
    {
        if (LobbyManager.instance && !LobbyManager.instance.IsDemoLobby || !LobbyManager.instance && SteamIntegration.instance && SteamIntegration.instance.IsFullVersion)
            if (destroyInstead)
                Destroy(gameObject);
            else
                gameObject.SetActive(false);
    }
}