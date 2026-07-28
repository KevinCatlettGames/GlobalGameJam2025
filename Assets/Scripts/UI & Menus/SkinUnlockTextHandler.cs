using UnityEngine;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;

public class SkinUnlockTextHandler : MonoBehaviour
{
    [SerializeField] GameObject achievementNameObject;
    [SerializeField] GameObject achievementDescriptionObject;
    public bool useUnlocking = false; 

    public void SetSkinUnlockText(SkinSO skinSO)
    {
        if(!skinSO.UnlockAchievement || !useUnlocking)
        {
            achievementNameObject.SetActive(false);
            achievementDescriptionObject.SetActive(false);
        }
        else if (skinSO.UnlockAchievement)
        {
            achievementNameObject.SetActive(true);
            achievementDescriptionObject.SetActive(true);
            achievementNameObject.GetComponent<LocalizeStringEvent>().StringReference = skinSO.UnlockAchievement.AchievementNameLocalization;
            achievementDescriptionObject.GetComponent<LocalizeStringEvent>().StringReference = skinSO.UnlockAchievement.AchievementDescriptionLocalization;
        }
    }
 }
