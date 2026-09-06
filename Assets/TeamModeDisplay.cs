using UnityEngine;
using System.Collections.Generic;

public class TeamModeDisplay : MonoBehaviour
{
    public static TeamModeDisplay Instance;

    [SerializeField] Transform[] playerBoxes;
    [SerializeField] GameObject[] vsTexts;
    [SerializeField] LobbyPlayerValues lobbyPlayerValues;
    List<Vector3> playerBoxPositions = new List<Vector3>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(this.gameObject);

            foreach (GameObject obj in vsTexts)
                obj.SetActive(false);

        foreach(Transform t in playerBoxes)
            playerBoxPositions.Add(t.localPosition);
    }

    public void SubscribeToTeamSwitchEvent(TeamSelection teamSelection)
    {
        teamSelection.OnTeamUpdated += BeginTeamModeDisplaying;
        Debug.Log("Subscribed to " + teamSelection.transform.name);
    }

    public void UnSubscribeToEvent(TeamSelection teamSelection)
    {
        teamSelection.OnTeamUpdated -= BeginTeamModeDisplaying;
        Debug.Log("Unsubed from " + teamSelection.transform.name);
    }

    void BeginTeamModeDisplaying()
    {
        Invoke(nameof(DisplayTeamMode), .5f);
    }

    void DisplayTeamMode()
    {
        Debug.Log("In Dynamic Team Mode Displaying");
        foreach (GameObject obj in vsTexts)
            obj.SetActive(false);

        if (LobbyManager.instance.SelectedGameMode != GameManager.GameModeType.Team)
            return;

        Debug.Log("Team Mode is active");
        int teamAAmount = 0;
        int teamBAmount = 0;

        foreach (LobbyPlayerValues.PlayerValues pv in lobbyPlayerValues.playerValuesList)
        {
            if (pv.TeamIndex == 0)
                teamAAmount++;
            else if (pv.TeamIndex == 1)
                teamBAmount++;
        }

        if (teamAAmount == 2 && teamBAmount == 2)
            vsTexts[1].SetActive(true);
        else
            vsTexts[0].SetActive(true);

        Debug.Log("Correct vs text activated");
    }
}