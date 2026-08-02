using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "new Achievement", menuName = "Scriptable Objects/SO_Achievement")]
public class SO_Achievement : ScriptableObject
{
    [SerializeField] private string achievementName;
    public string AchievementName { get { return achievementName; } }

    [SerializeField] private LocalizedString achievementNameLocalization;
    public LocalizedString AchievementNameLocalization
    {
        get => achievementNameLocalization;
        set => achievementNameLocalization = value;
    }

    [SerializeField] private LocalizedString achievementDescriptionLocalization;
    public LocalizedString AchievementDescriptionLocalization
    {
        get => achievementDescriptionLocalization;
        set => achievementDescriptionLocalization = value;
    }

    [SerializeField] private int achievementID;
    public int AchievementID { get { return achievementID; } }


    [SerializeField] private string statName;
    public string StatName { get { return statName; } }


    [SerializeField] private int statID;
    public int StatID { get { return statID; } }


    [SerializeField] private int statThreshold;
    public int StatThreshold { get { return statThreshold; } }
}