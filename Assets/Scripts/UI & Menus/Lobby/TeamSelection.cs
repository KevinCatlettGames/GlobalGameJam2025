using UnityEngine;
using UnityEngine.UI;
using EditorAttributes;
using TMPro;

public class TeamSelection : MonoBehaviour
{
    [SerializeField] private int playerIndex;
    public PlayerContainerManager playerContainerManager;

    [SerializeField, ReadOnly] private int currentTeamIndex;
    [SerializeField] private LobbyPlayerValues lobbyPlayerHandler;

    [SerializeField] private TextMeshProUGUI teamText;
    [SerializeField] private Image teamImage;

    private int maxTeamSize = 2;
    private bool initialSet = false;

    private void OnEnable()
    {
        Invoke(nameof(Init), 0.2f);
    }

    private void Init()
    {
        if (!initialSet)
        {
            initialSet = true;

            currentTeamIndex = playerIndex <= 1 ? 1 : 2;

            SetTeam();
        }

        UpdateTeamIndex((ulong)playerIndex);
        LobbyManager.instance.OnReadyStateUpdated.AddListener(UpdateTeamIndex);
    }

    private void OnDisable()
    {
        if (LobbyManager.instance != null &&
            LobbyManager.instance.SelectedGameMode == GameManager.GameModeType.Standard)
        {
            lobbyPlayerHandler.playerValuesList[playerIndex].TeamIndex = -1;
        }

        LobbyManager.instance.OnReadyStateUpdated.RemoveListener(UpdateTeamIndex);
    }

    public void ChangeTeam()
    {
        if (playerContainerManager.isReady)
            return;

        currentTeamIndex = currentTeamIndex == 1 ? 2 : 1;
        SetTeam();
    }

    private void SetTeam()
    {
        if (currentTeamIndex == 1)
        {
            teamImage.color = LobbyManager.instance.TeamColors[0];
            teamText.text = "T1";
        }
        else if (currentTeamIndex == 2)
        {
            teamImage.color = LobbyManager.instance.TeamColors[1];
            teamText.text = "T2";
        }
    }

    private void UpdateTeamIndex(ulong playerID)
    {
        if (playerID != (ulong)playerIndex)
            return;

        if (LobbyManager.instance.players[(int)playerID].IsReady)
        {
            if (currentTeamIndex == 1 || currentTeamIndex == 2)
            {
                lobbyPlayerHandler.playerValuesList[playerIndex].TeamIndex = currentTeamIndex;
            }
        }
        else
        {
            lobbyPlayerHandler.playerValuesList[playerIndex].TeamIndex = -1;
            SetTeam();
        }
    }
}