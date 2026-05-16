using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.UI; 

[CreateAssetMenu(fileName = "New Tutorial ItemSO", menuName = "Scriptable Objects/SO_TutorialItem")]
public class TutorialItemSO : ScriptableObject
{
    [Header("Video")]
    [SerializeField] private VideoClip itemClip;
    public VideoClip ItemClip
    {
        get => itemClip;
        set => itemClip = value;
    }

    [SerializeField] private Color clipOutlineColor;
    public Color ClipOutlineColor
    {
        get => clipOutlineColor;
        set => clipOutlineColor = value;
    }

    [SerializeField] private bool showClipOutline;
    public bool ShowClipOutline
    {
        get => showClipOutline;
        set => showClipOutline = value;
    }

    [Header("Images")]
    [SerializeField] private Sprite itemMainImage;
    public Sprite ItemMainImage
    {
        get => itemMainImage;
        set => itemMainImage = value;
    }

    [SerializeField] private Sprite itemSprite;
    public Sprite ItemSprite
    {
        get => itemSprite;
        set => itemSprite = value;
    }

    [Header("Stats")]
    [SerializeField] int[] stats;
    public int[] Stats
    {
        get => stats;
        set => stats = value;
    }

    [Header("Name Text")]
    [SerializeField] private string itemNameText;
    public string ItemNameText
    {
        get => itemNameText;
        set => itemNameText = value;
    }

    [SerializeField] private bool showitemNameText;
    public bool ShowItemNameText
    {
        get => showitemNameText;
        set => showitemNameText = value;
    }

    [Header("Name Image")]
    [SerializeField] private Sprite itemNameImage;
    public Sprite ItemNameImage
    {
        get => itemNameImage;
        set => itemNameImage = value;
    }

    [SerializeField] private bool showitemNameImage;
    public bool ShowItemNameImage
    {
        get => showitemNameImage;
        set => showitemNameImage = value;
    }

    [Header("Description")]
    [TextArea]
    [SerializeField] private string itemDescription;
    public string ItemDescription
    {
        get => itemDescription;
        set => itemDescription = value;
    }

    [Header("Settings")]
    [SerializeField] private bool useButton;
    public bool UseButton
    {
        get => useButton;
        set => useButton = value;
    }
    
    [SerializeField] private string buttonText;
    public string ButtonText
    {
        get => buttonText;
        set => buttonText = value;
    }
    
    [SerializeField] private bool incorporateMainElementBackground = true;
    public bool IncorporateMainElementBackground
    {
        get => incorporateMainElementBackground;
        set => incorporateMainElementBackground = value;
    }
}