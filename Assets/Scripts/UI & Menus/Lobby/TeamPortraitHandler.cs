using UnityEngine;
using UnityEngine.UI;

public class TeamPortraitHandler : MonoBehaviour
{
    public GameObject[] teamPortraits;
    public GameObject[] teamKillCounters;

    private void OnEnable()
    {
        if (GameManager.Instance.GameMode != GameManager.GameModeType.Team)
            return;

        foreach (GameObject team in teamKillCounters)
        {
            team.SetActive(true);
        }

        int teamACount = 0;
        int teamBCount = 0;

        for (int i = 0; i < LobbyPlayerHandler.Instance.playerValuesList.Count; i++)
        {
            var playerData = LobbyPlayerHandler.Instance.playerValuesList[i];

            if (playerData == null)
                continue;

            if (playerData.TeamIndex == 1 && teamACount < 2)
            {
                teamPortraits[teamACount].SetActive(true);
                teamPortraits[teamACount]
                    .GetComponent<Image>()
                    .sprite = playerData.Skin.GameSprites[0];

                teamACount++;
            }
            else if (playerData.TeamIndex == 2 && teamBCount < 2)
            {
                int portraitIndex = teamBCount + 2;

                teamPortraits[portraitIndex].SetActive(true);
                teamPortraits[portraitIndex]
                    .GetComponent<Image>()
                    .sprite = playerData.Skin.GameSprites[0];

                teamBCount++;
            }
        }
    }
}