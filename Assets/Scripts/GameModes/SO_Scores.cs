using UnityEngine;

[CreateAssetMenu(fileName = "SO_Scores", menuName = "Scriptable Objects/SO_Scores")]
public class SO_Scores : ScriptableObject
{
    public int[] WinScores = new int[4];
    public int[] KillScores = new int[4];

    public void ResetScores()
    {
        WinScores = new int[4];
        KillScores = new int[4];
    }
}
