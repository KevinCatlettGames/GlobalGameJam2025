using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

public class SkinUnlockedUI : MonoBehaviour
{
    private AchievementSaveSystem aSaveS;

    [Header("Untoggle to disable this feature")]
    [SerializeField] private bool showWindow = true;
    [Space]

    [SerializeField] private SkinSO[] possibleSkins;
    [SerializeField] private float duration = 5f;

    [SerializeField] private GameObject unlockPanelParent;
    [SerializeField] private Image skinImage;
    [SerializeField] private LocalizeStringEvent skinNameLocalizer;
    [SerializeField] private LocalizeStringEvent descriptionLocalizer;
    [SerializeField] private Outline outline; 

    private void OnEnable()
    {
        if (AchievementSaveSystem.instance)
        {
            aSaveS = AchievementSaveSystem.instance;
            aSaveS.OnAchievementUnlocked += EvaluateSkinUnlock;
        }
    }

    private void OnDisable()
    {
        if (aSaveS)
            aSaveS.OnAchievementUnlocked -= EvaluateSkinUnlock;
    }

    private void EvaluateSkinUnlock(int achievementIndex)
    {
        if (!showWindow) return;

        //Debug.Log("In evaluate achievement " + achievementIndex);
        SO_Achievement newlyUnlockedAchievement = null;
        foreach (SO_Achievement ach in aSaveS.AchievementList)
        {
            if (ach.AchievementID == achievementIndex)
            {
                newlyUnlockedAchievement = ach;
                break;
            }
        }
        if (!newlyUnlockedAchievement) return;
        //Debug.Log("achievement found");
        SkinSO newlyUnlockedSkin = null;
        foreach (SkinSO skin in possibleSkins)
        {
            if (skin.UnlockAchievement && skin.UnlockAchievement.AchievementID == newlyUnlockedAchievement.AchievementID)
            {
                newlyUnlockedSkin = skin;
                break;
            }
        }
        if (!newlyUnlockedSkin) return;
        //Debug.Log("Skin found");
        ShowSkinUnlock(newlyUnlockedAchievement, newlyUnlockedSkin);
    }

    private void ShowSkinUnlock(SO_Achievement ach, SkinSO skin)
    {
        unlockPanelParent.SetActive(false);
        unlockPanelParent.SetActive(true);

        if (skinImage) skinImage.sprite = skin.SplashArt;
        if (outline) outline.effectColor = skin.Color;

        if (skinNameLocalizer) skinNameLocalizer.StringReference = ach.AchievementNameLocalization;
        if (descriptionLocalizer) descriptionLocalizer.StringReference = ach.AchievementDescriptionLocalization;

        Invoke(nameof(HideSkinUnlock), duration);
    }

    private void HideSkinUnlock()
    {
        //Debug.Log("Hiding skin unlock");
        unlockPanelParent.SetActive(false); 
    }
}