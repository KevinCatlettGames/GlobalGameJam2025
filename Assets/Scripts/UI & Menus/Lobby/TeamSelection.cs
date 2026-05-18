using UnityEngine;
using UnityEngine.UI;
using EditorAttributes;
using TMPro;
using Unity.VisualScripting;

public class TeamSelection : MonoBehaviour
{
    [SerializeField] private int playerIndex;
    public PlayerContainerManager playerContainerManager;
    public PlayerContainerSkinChange playerContainerSkinChange;

    [SerializeField, ReadOnly] private int currentTeamIndex;
    public int CurrentTeamIndex
    {  get { return currentTeamIndex; } }

    [SerializeField] private LobbyPlayerValues lobbyPlayerHandler;

    [SerializeField] private TextMeshProUGUI teamText;
    [SerializeField] private Image teamImage;

    private int maxTeamSize = 2;
    private bool initialSet = false;

    private void OnEnable()
    {
        Invoke(nameof(Init), 0.2f);
    }
    private void OnDisable()
    {
        if (LobbyManager.instance != null &&
            LobbyManager.instance.SelectedGameMode == GameManager.GameModeType.Standard)
        {
            lobbyPlayerHandler.playerValuesList[playerIndex].TeamIndex = -1;
        }

        LobbyManager.instance.OnReadyStateUpdated.RemoveListener(UpdateTeamIndex);
        playerContainerSkinChange.UpdateBlur();
        currentTeamIndex = -1;
        initialSet = false;
    }

    private void Init()
    {
        if (!initialSet)
        {
            initialSet = true;

            int playersInTeamA = 0;
            int playersInTeamB = 0;

            foreach (GameObject teamSelection in LobbyManager.instance.teamSelections)
            {
                if (teamSelection == gameObject) continue;
                if (teamSelection.GetComponent<TeamSelection>().currentTeamIndex == 1)
                    playersInTeamA++;
            }

            foreach (GameObject teamSelection in LobbyManager.instance.teamSelections)
            {
                if (teamSelection == gameObject) continue;
                if (teamSelection.GetComponent<TeamSelection>().currentTeamIndex == 2)
                    playersInTeamB++;
            }

            if(playersInTeamA < playersInTeamB)
                currentTeamIndex = 1;
            else if(playersInTeamB < playersInTeamA)
                currentTeamIndex = 2;
            else if(playersInTeamA ==  playersInTeamB)
                currentTeamIndex = 1;

            SetTeam();
        }

        UpdateTeamIndex((ulong)playerIndex, true);
        LobbyManager.instance.OnReadyStateUpdated.AddListener(UpdateTeamIndex);
    }

    public void ChangeTeam()
    {
        if (playerContainerManager.isReady)
            return;

        int potentialNewTeamID = -1;
        if (currentTeamIndex == 1)
            potentialNewTeamID = 2;
        else if (currentTeamIndex == 2)
            potentialNewTeamID = 1;

        int playersInPotentialTeam = 0;

        foreach (GameObject teamSelection in LobbyManager.instance.teamSelections)
        {
            if (teamSelection == gameObject) continue;
            if(teamSelection.GetComponent<TeamSelection>().currentTeamIndex == potentialNewTeamID)
                playersInPotentialTeam++;
        }

        if (playersInPotentialTeam > 2)
            return;

        currentTeamIndex = currentTeamIndex == 1 ? 2 : 1;
        SetTeam();
    }

    private void SetTeam()
    {
        teamImage.enabled = true;
        teamText.enabled = true;

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
        playerContainerSkinChange.UpdateBlur();
    }

    private void UpdateTeamIndex(ulong playerID, bool state)
    {
        if (playerID != (ulong)playerIndex)
            return;

        if (state)
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