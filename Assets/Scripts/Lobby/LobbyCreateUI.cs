using UnityEngine;
using UnityEngine.UI;

public class LobbyCreateUI : MonoBehaviour
{
    [SerializeField] private Button createPublicButton;

    private void Awake()
    {
        createPublicButton.onClick.AddListener(() =>
        {
            GameLobby.instance.CreateLobby("Empty", false);
            createPublicButton.interactable = false;
        });
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }
}