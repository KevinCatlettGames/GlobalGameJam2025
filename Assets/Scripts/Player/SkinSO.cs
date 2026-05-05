using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "New SkinSO", menuName = "Scriptable Objects/SO_Skin")]
public class SkinSO : ScriptableObject
{
    [FormerlySerializedAs("sprite")] 
    [SerializeField] private Sprite[] gameSprites;
    [SerializeField] private Sprite lobbySprite;
    [SerializeField] private Color color;
    [SerializeField] private int index; 
    [SerializeField] private GameObject skinPrefab;
    [SerializeField] private bool availableInDemo;

    public Sprite[] GameSprites => gameSprites;
    
    public Sprite LobbySprite => lobbySprite;
    
    public Color Color => color;

    public int Index => index;

    public GameObject SkinPrefab => skinPrefab;

    public bool AvailableInDemo => availableInDemo;
}