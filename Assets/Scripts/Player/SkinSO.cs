using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "New SkinSO", menuName = "Scriptable Objects/SO_Skin")]
public class SkinSO : ScriptableObject
{
    [FormerlySerializedAs("sprite")] 
    [SerializeField] private Sprite[] headSprites;
    [SerializeField] private Sprite splashArt;
    [SerializeField] private Color color;
    [SerializeField] private int index; 
    [SerializeField] private GameObject skinPrefab;
    [SerializeField] private bool availableInDemo;
    [SerializeField] private SO_Achievement unlockAchievement;
    public SO_Achievement UnlockAchievement {  get { return unlockAchievement; } }

    public Sprite[] HeadSprites => headSprites;
    
    public Sprite SplashArt => splashArt;
    
    public Color Color => color;

    public int Index => index;

    public GameObject SkinPrefab => skinPrefab;

    public bool AvailableInDemo => availableInDemo;
}