using UnityEngine;

[CreateAssetMenu(fileName = "SO_GameSettings", menuName = "Scriptable Objects/SO_GameSettings")]
public class SO_GameSettings : ScriptableObject
{
    [SerializeField] private int lifes = 3;
    public int Lifes {  get { return lifes; } }

    [SerializeField] private float time = 300f;
    public float Time { get { return time; } }
}
