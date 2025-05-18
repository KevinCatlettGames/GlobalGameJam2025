using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyCreateUI : MonoBehaviour
{
    [SerializeField] private Button createPublicButton;
    
    private void Start()
    {
    }

    private void Awake()
    {
        createPublicButton.onClick.AddListener(() =>
        {
            GameLobby.instance.CreateLobby("Empty", false);
            createPublicButton.enabled = false; 
        });
    }

    public void Show()
    {
        gameObject.SetActive(true);
    }
}
