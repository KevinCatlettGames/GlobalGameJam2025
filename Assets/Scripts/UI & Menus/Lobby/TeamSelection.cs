using UnityEngine;
using UnityEngine.UI;
using EditorAttributes;

public class TeamSelection : MonoBehaviour
{
    [SerializeField] private int playerIndex;
    public PlayerContainerManager playerContainerManager;

    [SerializeField, ReadOnly] private int currentTeamIndex;
    [SerializeField] private LobbyPlayerHandler lobbyPlayerHandler;

    [SerializeField] private Image teamImage;
    [SerializeField] private Sprite[] teamSprites;
    [SerializeField] private Color teamAColor;
    [SerializeField] private Color teamBColor;

    private int maxTeamSize = 2;
    private bool initialSet = false;

    private bool setTeamIsValid = true;
    public bool SetTeamIsValid { get { return setTeamIsValid; } }

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

            SetTeamUI();
            Invoke(nameof(HandleTeamValidity), 0.1f);
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

        Invoke(nameof(HandleTeamValidity), 0.1f);
        SetTeamUI();
    }

    private void SetTeamUI()
    {
        if (currentTeamIndex == 1)
        {
            teamImage.sprite = teamSprites[0];
        }
        else if (currentTeamIndex == 2)
        {
            teamImage.sprite = teamSprites[1];
        }
    }

    private void UpdateTeamIndex(ulong playerID)
    {
        if (!LobbyManager.instance.players[playerIndex].IsReady)
        {
            Invoke(nameof(HandleTeamValidity), 0.1f);
        }

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
            SetTeamUI();
            Invoke(nameof(HandleTeamValidity), 0.1f);
        }
    }

    public void HandleTeamValidity()
    {
        int currentTeamAmount = 0;

        foreach (LobbyPlayerHandler.PlayerValues player in lobbyPlayerHandler.playerValuesList)
        {
            if (player.TeamIndex == currentTeamIndex)
                currentTeamAmount++;
        }

        if (currentTeamAmount >= maxTeamSize)
        {
            setTeamIsValid = false;
            teamImage.color = Color.red;
        }
        else if (currentTeamIndex == 1)
        {
            setTeamIsValid = true;
            teamImage.color = teamAColor;
        }
        else if (currentTeamIndex == 2)
        {
            setTeamIsValid = true;
            teamImage.color = teamBColor;
        }
    }
}