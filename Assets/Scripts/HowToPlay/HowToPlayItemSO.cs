using UnityEngine;
using UnityEngine.Video;

[CreateAssetMenu(fileName = "New HowToPlay ItemSO", menuName = "Scriptable Objects/SO_HowToPlayItem")]
public class HowToPlayItemSO : ScriptableObject
{
    [SerializeField] private VideoClip itemClip;
    public VideoClip ItemClip
    {
        get => itemClip;
        set => itemClip = value;
    }

    [SerializeField] Sprite itemMainImage;
    public Sprite ItemMainImage
    {
        get => itemMainImage;
        set => itemMainImage = value;
    }

    private string itemName;
    public string ItemName
    {
        get => itemName;
        set => itemName = value;
    }

    [SerializeField] Sprite itemSprite;
    public Sprite ItemSprite
    {
        get => itemSprite;
        set => itemSprite = value;
    }

    [SerializeField] private string itemDescription;
    public string ItemDescription
    {
        get => itemDescription;
        set => itemDescription = value;
    }
}