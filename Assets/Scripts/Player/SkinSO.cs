using UnityEngine;

[CreateAssetMenu(fileName = "New SkinSO", menuName = "SkinSO")]
public class SkinSO : ScriptableObject
{
    [SerializeField] private Sprite sprite;
    [SerializeField] private Color color;
    [SerializeField] private int index; 
    public Sprite Sprite => sprite;
    public Color Color => color;
    public int Index => index;
}