using UnityEngine;

public class TestingGM : GameManager
{
    [SerializeField] private GameObject endTutorialObject;
    private bool endTutorial = false;

    public override void DeathReportLocal(int playerID, int killCredit, bool isSuperKO)
    {
        if (playerID == 5 && !endTutorial && endTutorialObject != null)
        {
            endTutorialObject.SetActive(true);
            endTutorial = true;
        }
        base.DeathReportLocal(playerID, killCredit, isSuperKO);
    }
}