using UnityEngine;

public class DisableOnFullVersion : MonoBehaviour
{
    public bool destroyInstead = false;
    void OnEnable()
    {
        Invoke(nameof(Evaluate), .5f);
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