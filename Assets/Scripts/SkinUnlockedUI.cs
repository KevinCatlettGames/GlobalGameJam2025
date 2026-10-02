using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;
using FMODUnity;

public class SkinUnlockedUI : MonoBehaviour
{
    private AchievementSaveSystem aSaveS;

    [Header("Untoggle to disable this feature")]
    [SerializeField] private bool showWindow = true;
    [Space]

    [SerializeField] private SkinSO[] possibleSkins;
    [SerializeField] private float duration = 5f;
    [SerializeField] private float bufferTime = 0.5f; // Pause between queued notifications

    [SerializeField] private GameObject unlockPanelParent;
    [SerializeField] private Image skinImage;
    [SerializeField] private LocalizeStringEvent skinNameLocalizer;
    [SerializeField] private LocalizeStringEvent descriptionLocalizer;
    [SerializeField] private TextMeshProUGUI thresholdText;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private StudioEventEmitter emitter;

    private Queue<(SO_Achievement ach, SkinSO skin)> unlockQueue = new Queue<(SO_Achievement, SkinSO)>();
    private Coroutine processQueueCoroutine;

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

        if (processQueueCoroutine != null)
            StopCoroutine(processQueueCoroutine);

        unlockQueue.Clear();
    }

    private void EvaluateSkinUnlock(int achievementIndex)
    {
        if (!showWindow) return;

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

        unlockQueue.Enqueue((newlyUnlockedAchievement, newlyUnlockedSkin));

        if (processQueueCoroutine == null)
        {
            processQueueCoroutine = StartCoroutine(ProcessUnlockQueue());
        }
    }

    private IEnumerator ProcessUnlockQueue()
    {
        while (unlockQueue.Count > 0)
        {
            var (ach, skin) = unlockQueue.Dequeue();

            DisplaySkinUnlock(ach, skin);

            yield return new WaitForSeconds(duration);

            unlockPanelParent.SetActive(false);

            if (unlockQueue.Count > 0 && bufferTime > 0f)
            {
                yield return new WaitForSeconds(bufferTime);
            }
        }

        processQueueCoroutine = null;
    }

    private void DisplaySkinUnlock(SO_Achievement ach, SkinSO skin)
    {
        unlockPanelParent.SetActive(false);
        unlockPanelParent.SetActive(true);
        //emitter.Play();
        if (skinImage) skinImage.sprite = skin.SplashArt;
        if (backgroundImage) backgroundImage.color = skin.Color;

        if (skinNameLocalizer) skinNameLocalizer.StringReference = ach.AchievementNameLocalization;
        if (descriptionLocalizer) descriptionLocalizer.StringReference = ach.AchievementDescriptionLocalization;

        if (ach.StatThreshold > 0)
        {
            thresholdText.text = $"{ach.StatThreshold}x";
        }
        else
        {
            thresholdText.text = string.Empty;
        }
    }
}