using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "New SkinSO", menuName = "Scriptable Objects/SO_Skin")]
public class SkinSO : ScriptableObject
{
    [FormerlySerializedAs("sprite")] [SerializeField] private Sprite gameSprite;
    [SerializeField] private Sprite lobbySprite;
    [SerializeField] private Color color;
    [SerializeField] private int index; 
    public Sprite GameSprite => gameSprite;
    
    public Sprite LobbySprite => lobbySprite;
    
    public Color Color => color;
    public int Index => index;
}