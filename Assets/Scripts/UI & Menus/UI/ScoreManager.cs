using System.Collections;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;

    [SerializeField] private ScorePanel[] scorePanels;
    [SerializeField] private PlayerHUD[] playerHUDs;
    [SerializeField] private SO_Scores scores;
    [SerializeField] private GameObject restarText;
    [SerializeField] private GameObject scoreScreen;
    [SerializeField] private GameObject winScreen;
    private int[] pendingWins = new int[4];
    private int[] pendingKills = new int[4];
    private int currentActivePlayers = 0;

    private bool scoresResolved = false;
    public bool ScoresResolved {  get { return scoresResolved; } }

    public bool showWinner = false;
    
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
    }
    public void InitialiseScorePanel(int playerID, Sprite playerPortrait, Color playerColor)
    {
        currentActivePlayers++;
        ScorePanel scorePanel = scorePanels[playerID];
        scorePanel.gameObject.SetActive(true);
        scorePanel.SetPortrait(playerPortrait, playerColor);
        
        for (int i = 0; i < currentActivePlayers; i++)
        {
            int kills = scores.KillScores[i];
            int wins = scores.WinScores[i];
            scorePanels[i].SetScores(wins, kills);
        }
    }
    public void AddPendingScore(int playerID, bool isWin)
    {
        scoresResolved = false;
        if (isWin)
        {
            pendingWins[playerID]++;
            scores.WinScores[playerID]++;
        }
        else
        {
            pendingKills[playerID]++;
            scores.KillScores[playerID]++;
        }
    }
    public void ResolveScores()
    {
        StartCoroutine(ResolveScoresCoroutine());
    }
    public IEnumerator ResolveScoresCoroutine()
    {
        if(currentActivePlayers <= 0 || currentActivePlayers > 4) yield break;
        restarText.SetActive(false);
        for (int i = 0; i < currentActivePlayers; i++)
        {
            int kills = scores.KillScores[i] - pendingKills[i];
            int wins = scores.WinScores[i] - pendingWins[i];
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
        yield return new WaitForSeconds(2f);
        
        if (showWinner)
        {
            restarText.SetActive(false);
            winScreen.SetActive(true);
            scoreScreen.SetActive(false);
        }
        else
        {
            pendingKills = new int[4];
            pendingWins = new int[4];
            restarText.SetActive(true);
        }

        scoresResolved = true;
    }
    public void ResetScores()
    {
        scores.ResetWins();
        scores.ResetKills();
    }
    public int[] GetKillScores()
    {
        return scores.KillScores;
    }
    
    private void OnApplicationQuit()
    {
        ResetScores();
    }
}