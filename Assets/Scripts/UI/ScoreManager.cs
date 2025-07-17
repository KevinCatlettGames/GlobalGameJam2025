using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [SerializeField] private ScorePanel[] scorePanels;
    [SerializeField] private SO_Scores Scores;
    [SerializeField] private GameObject restarText;

    private int[] pendingWins = new int[4];
    private int[] pendingKills = new int[4];
    private int currentActivePlayers = 0;

    private bool scoresResolved = false;
    public bool ScoresResolved {  get { return scoresResolved; } }

    void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this);
        }
        Scores.ResetScores();
    }
    public void InitialiseScorePanel(int playerID, Sprite playerPortrait)
    {
        currentActivePlayers++;
        ScorePanel scorePanel = scorePanels[playerID];
        scorePanel.gameObject.SetActive(true);
        scorePanel.SetPortrait(playerPortrait);
    }
    public void AddPendingScore(int playerID, bool isWin)
    {
        scoresResolved = false;
        if (isWin)
        {
            pendingWins[playerID]++;
            Scores.winScores[playerID]++;
        }
        else
        {
            pendingKills[playerID]++;
            Scores.killScores[playerID]++;
        }
    }
    public void ResolveScores()
    {
        StartCoroutine(ResolveScoresCoroutine());
    }
    public IEnumerator ResolveScoresCoroutine()
    {
        Debug.Log("ResolveScores");
        if(currentActivePlayers <= 0 || currentActivePlayers > 4) yield break;
        restarText.SetActive(false);
        for (int i = 0; i < currentActivePlayers; i++)
        {
            int kills = Scores.killScores[i] - pendingKills[i];
            int wins = Scores.winScores[i] - pendingWins[i];
            scorePanels[i].SetScores(wins, kills);
        }
        yield return new WaitForSeconds(.2f);
        for (int i = 0; i < currentActivePlayers; i++)
        {
            for (int w = 0; w < pendingWins[i]; w++)
            {
                scorePanels[i].AddWin();
                yield return new WaitForSeconds(.1f);
            }
            for (int k = 0; k < pendingKills[i]; k++)
            {
                scorePanels[i].AddKill();
                yield return new WaitForSeconds(.1f);
            }
            yield return new WaitForSeconds(.2f);
        }
        pendingKills = new int[4];
        pendingWins = new int[4];
        restarText.SetActive(true);
        scoresResolved = true;
    }
    public void ResetScores()
    {
        Scores.ResetScores();
    }
}
