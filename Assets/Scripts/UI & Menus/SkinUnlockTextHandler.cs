using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Localization.Components;

public class SkinUnlockTextHandler : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private GameObject achievementDescriptionObject;
    [SerializeField] private Slider achievementStatSlider;
    [SerializeField] private GameObject background;
    [SerializeField] private TextMeshProUGUI statText;

    [Header("Settings")]
    [SerializeField] private bool useUnlocking = false;

    private LocalizeStringEvent _descriptionLocalizer;

    private void Awake()
    {
        if (achievementDescriptionObject != null)
        {
            _descriptionLocalizer = achievementDescriptionObject.GetComponent<LocalizeStringEvent>();
        }
    }

    public void SetSkinUnlockText(SkinSO skinSO)
    {
        if(SteamIntegration.instance && !SteamIntegration.instance.IsFullVersion)
        {
            HideUnlockUI();
            return;
        }    


        if (skinSO == null)
        {
            HideUnlockUI();
            return;
        }

        var achievement = skinSO.UnlockAchievement;

        if (!useUnlocking || achievement == null)
        {
            HideUnlockUI();
            return;
        }

        bool isUnlocked = AchievementSaveSystem.instance != null &&
                          AchievementSaveSystem.instance.IsAchievementUnlocked(achievement.AchievementID);

        if (isUnlocked)
        {
            HideUnlockUI();
            return;
        }

        ShowAchievementDetails(achievement);
    }

    private void ShowAchievementDetails(SO_Achievement achievement)
    {
        background.SetActive(true);
        achievementDescriptionObject.SetActive(true);

        if (_descriptionLocalizer != null)
        {
            _descriptionLocalizer.StringReference = achievement.AchievementDescriptionLocalization;
        }

        bool hasStatProgress = achievement.StatID != -1;
        achievementStatSlider.gameObject.SetActive(hasStatProgress);

        if (hasStatProgress && AchievementSaveSystem.instance != null)
        {
            int currentValue = AchievementSaveSystem.instance.GetStatInt(achievement.StatName);
            int targetValue = achievement.StatThreshold;

            achievementStatSlider.maxValue = targetValue;
            achievementStatSlider.value = currentValue;
            statText.text = $"{currentValue}/{targetValue}";
        }
        else
        {
            statText.text = string.Empty;
        }
    }

    private void HideUnlockUI()
    {
        if (achievementDescriptionObject != null) achievementDescriptionObject.SetActive(false);
        if (achievementStatSlider != null) achievementStatSlider.gameObject.SetActive(false);
        if (background != null) background.SetActive(false);
        if (statText != null) statText.text = string.Empty;
    }
}