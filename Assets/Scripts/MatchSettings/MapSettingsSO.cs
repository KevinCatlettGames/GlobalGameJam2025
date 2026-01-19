using UnityEngine;

[CreateAssetMenu(fileName = "MapSettingsSO", menuName = "Scriptable Objects/SO_MapSettings")]
public class MapSettingsSO : ScriptableObject
{
    /// <summary>
    /// The map id used to compare and get the correct MapSettingsSO.
    /// </summary>
    [SerializeField] private int mapID;
    public int MapID { get => mapID; set => mapID = value; }

    /// <summary>
    /// Should this map be used?
    /// </summary>
    [SerializeField] private bool playMap = true;
    public bool PlayMap { get => playMap; set => playMap = value; }

    /// <summary>
    /// Should the specific map event be featured?
    /// </summary>
    [SerializeField] private bool playWithMapEvent = true;
    public bool PlayWithMapEvent { get => playWithMapEvent; set => playWithMapEvent = value; }
    
    /// <summary>
    /// Should the specific map event be featured?
    /// </summary>
    [SerializeField] private int mapRounds = 3;
    public int MapRounds { get => mapRounds; set => mapRounds = value; }

    /// <summary>
    /// Should the specific map event be featured?
    /// </summary>
    private bool playedThisLoop = false;
    public bool PlayedThisLoop { get => playedThisLoop; set => playedThisLoop = value; }
    
    /// <summary>
    /// The name of the scene file, used for loading the correct scene.
    /// </summary>
    [SerializeField] private string sceneName;
    public string SceneName { get => sceneName; set => sceneName = value; }
}