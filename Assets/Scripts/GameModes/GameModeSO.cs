using UnityEngine;
using UnityEngine.Localization.PropertyVariants.TrackedProperties;

[CreateAssetMenu(fileName = "GameModeSO", menuName = "Scriptable Objects/SO_GameMode")]
public class GameModeSO : ScriptableObject
{
    [SerializeField] GameManager.GameModeType gameModeType;
    public GameManager.GameModeType GameModeType { get => gameModeType; set => gameModeType = value; }
    
    [SerializeField] string gamemodeTypeName;
    public string GamemodeTypeName { get => gamemodeTypeName; set => gamemodeTypeName = value; }

    [SerializeField] private LocalizedStringProperty gameModeLocalizationProperty; 
    public LocalizedStringProperty GameModeLocalizationProperty { get => gameModeLocalizationProperty; set => gameModeLocalizationProperty = value; }
    
    [SerializeField] Sprite gameModeTypeImage;
    public Sprite GameModeTypeImage { get => gameModeTypeImage; set => gameModeTypeImage = value; }
}