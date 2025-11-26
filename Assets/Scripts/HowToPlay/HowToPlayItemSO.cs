using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Video;
using UnityEngine.UI; 

[CreateAssetMenu(fileName = "New HowToPlay ItemSO", menuName = "Scriptable Objects/SO_HowToPlayItem")]
public class HowToPlayItemSO : ScriptableObject
{
    [Header("Video")]
    [SerializeField] private VideoClip itemClip;
    public VideoClip ItemClip
    {
        get => itemClip;
        set => itemClip = value;
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

    [Header("Text")]
    [SerializeField] private string itemName;
    public string ItemName
    {
        get => itemName;
        set => itemName = value;
    }

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
    
    [SerializeField] private string levelToLoad;
    public string LevelToLoad
    {
        get => levelToLoad;
        set => levelToLoad = value;
    }
    
    [SerializeField] private string buttonText;
    public string ButtonText
    {
        get => buttonText;
        set => buttonText = value;
    }

    public void SetUpButtonWithLevelLoading(Button button)
    {
        button.onClick.AddListener(() => MainMenuLobbyCreator.Instance.StartSceneLocal(levelToLoad));
    }
}