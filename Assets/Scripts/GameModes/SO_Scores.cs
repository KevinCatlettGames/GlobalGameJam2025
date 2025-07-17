using UnityEngine;

[CreateAssetMenu(fileName = "SO_Scores", menuName = "Scriptable Objects/SO_Scores")]
public class SO_Scores : ScriptableObject
{
    public int[] winScores = new int[4];
    public int[] killScores = new int[4];

    public void ResetScores()
    {
        winScores = new int[4];
        killScores = new int[4];
    }
}
