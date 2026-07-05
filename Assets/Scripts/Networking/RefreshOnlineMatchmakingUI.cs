using TMPro;
using UnityEngine;

public class RefreshOnlineMatchmakingUI : MonoBehaviour
{
    [SerializeField] GameObject onlineMatchmakingUI;
    [SerializeField] GameObject joiningLobbyUI;
    [SerializeField] TMP_InputField publicServerNameInputField;
    [SerializeField] TMP_InputField lobbyCodeInputField;

    private void OnDisable()
    {
        Refresh();
    }

    public void Refresh()
    {
        onlineMatchmakingUI.SetActive(true);
        joiningLobbyUI.SetActive(false);
        joiningLobbyUI.GetComponent<TextMeshProUGUI>().enabled = false;
        publicServerNameInputField.text = string.Empty;
        publicServerNameInputField.placeholder.gameObject.SetActive(true);
        lobbyCodeInputField.text = string.Empty;
        lobbyCodeInputField.placeholder.gameObject.SetActive(true);
    }
}