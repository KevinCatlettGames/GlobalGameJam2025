using UnityEngine;

[CreateAssetMenu(fileName = "SO_Scores", menuName = "Scriptable Objects/SO_Scores")]
public class SO_Scores : ScriptableObject
{
    public int[] WinScores = new int[4];
    public int[] teamWinScores = new int[2];
    public int[] KillScores = new int[4];

    [Header("Death Tracking")]
    public int[] DeathScores = new int[4];      // Individual player deaths
    public int[] TeamDeathScores = new int[2];  // Team deaths

    public void ResetWins()
    {
        WinScores = new int[4];
        teamWinScores = new int[2];
    }

    public void ResetKills()
    {
        KillScores = new int[4];
    }

    public void ResetDeaths()
    {
        DeathScores = new int[4];
        TeamDeathScores = new int[2];
    }

    public void ResetAll()
    {
        ResetWins();
        ResetKills();
        ResetDeaths();
    }
}