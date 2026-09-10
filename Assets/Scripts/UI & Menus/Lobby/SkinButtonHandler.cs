using FMODUnity;
using Steamworks;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SkinButtonHandler : MonoBehaviour
{
    public SkinButtonHandler rightSkinSelection;
    public SkinButtonHandler leftSkinSelection;
    public SkinButtonHandler topSkinSelection;
    public SkinButtonHandler bottomSkinSelection;

    public int skinButtonHandlerIndex = -1;
    public int skinAchievementIndex = -1;
    public Image[] selectionimages;
    public TextMeshProUGUI[] selectionTexts;

    public SkinSO skinSo;
    public Image skinImage;

    public Color standardImageColor = Color.gray;
    public Color disabledColor = Color.red;

    public Vector3 originalScale;
    public float scaleMultiplier;

    private int hoveredAmount = 0;
    private bool[] isHovering;

    private List<int> activePlayers = new List<int>();

    bool didFirstInit;
    public RawImage[] avatarImages;

    PlayerContainerManager currentPlayerContainerManager;

    Coroutine fadeCoroutine;

    [Header("Target Components")]
    [SerializeField] private RectTransform targetTransform;

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 1.2f;
    [SerializeField] private AnimationCurve colorEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Impact Juice (Relative Scale Multipliers)")]
    [SerializeField] private float startScaleMultiplier = 0.5f;
    [SerializeField] private float peakScaleMultiplier = 1.2f;
    [SerializeField] private float startRotation = -15f;

    // Cached initial scale to preserve aspect ratio & UI layout
    private Vector3 baseLocalScale;

    private void Awake()
    {
        originalScale = transform.localScale;
        isHovering = new bool[selectionimages.Length];

        if (targetTransform == null && skinImage != null)
        {
            targetTransform = skinImage.rectTransform;
        }

        if (targetTransform != null)
        {
            baseLocalScale = targetTransform.localScale;
        }
    }

    private void OnEnable()
    {
        if (didFirstInit) return;

        foreach (Image image in selectionimages)
        {
            image.enabled = false;
            image.color = Color.white;
        }

        foreach (TextMeshProUGUI text in selectionTexts)
            text.enabled = false;

        if (!SteamIntegration.instance.IsFullVersion && !skinSo.AvailableInDemo
            || AchievementSaveSystem.instance && skinSo.UnlockAchievement && !AchievementSaveSystem.instance.IsAchievementUnlocked(skinSo.UnlockAchievement.AchievementID))
        {
            skinImage.color = Color.black;
            GetComponent<Image>().color = disabledColor;
        }
        else
        {
            skinImage.color = Color.white;
            GetComponent<Image>().color = standardImageColor;
        }
        didFirstInit = true;
    }

    public void ChangePlayerIcon(int amount, int playerIndex, PlayerContainerManager playerContainerManager)
    {
        if (playerIndex < 0 || playerIndex >= selectionimages.Length)
            return;

        bool entering = amount > 0;

        if (isHovering[playerIndex] == entering)
            return;

        isHovering[playerIndex] = entering;

        if (entering)
        {
            if (!activePlayers.Contains(playerIndex))
                activePlayers.Add(playerIndex);
        }
        else
        {
            activePlayers.Remove(playerIndex);
        }

        hoveredAmount = activePlayers.Count;
        currentPlayerContainerManager = playerContainerManager;
        RefreshUI();
    }

    private void RefreshUI()
    {
        for (int i = 0; i < selectionimages.Length; i++)
        {
            selectionimages[i].enabled = false;
            selectionTexts[i].enabled = false;
            avatarImages[i].transform.parent.gameObject.SetActive(false);
            avatarImages[i].texture = null;
        }

        for (int slotIndex = 0; slotIndex < activePlayers.Count; slotIndex++)
        {
            int playerIndex = activePlayers[slotIndex];

            selectionimages[slotIndex].enabled = true;
            selectionimages[slotIndex].color = skinSo.Color;

            selectionTexts[slotIndex].enabled = true;
            selectionTexts[slotIndex].text = "P" + (playerIndex + 1);

#if !UNITY_SWITCH
            if (TransportSwitcher.Instance.isUsingRelay && SteamClient.IsValid)
            {
                avatarImages[slotIndex].transform.parent.gameObject.SetActive(true);
                avatarImages[slotIndex].texture = LobbyManager.instance.playerContainers[playerIndex].GetComponent<PlayerContainerManager>().playerProfileDisplay.cachedAvatar;
            }
#endif
        }

        bool hasHover = activePlayers.Count > 0;

        if (hasHover)
        {
            GetComponent<Outline>().effectColor = skinSo.Color;
            transform.localScale = originalScale * scaleMultiplier;
        }
        else
        {
            GetComponent<Outline>().effectColor = new Color(0, 0, 0, 0);
            transform.localScale = originalScale;
        }
    }

    public void ToggleReadyVisuals()
    {
        bool isSelectedNow = GetComponent<Image>().color != skinSo.Color;

        if (isSelectedNow)
        {
            if (!SteamIntegration.instance.IsFullVersion && !skinSo.AvailableInDemo || AchievementSaveSystem.instance && skinSo.UnlockAchievement && !AchievementSaveSystem.instance.IsAchievementUnlocked(skinSo.UnlockAchievement.AchievementID))
            {
                skinImage.color = Color.black;
                GetComponent<Image>().color = disabledColor;
            }
            else
            {
                skinImage.color = Color.white;
                GetComponent<Image>().color = skinSo.Color;
            }
        }
        else
        {
            if (!SteamIntegration.instance.IsFullVersion && !skinSo.AvailableInDemo || AchievementSaveSystem.instance && skinSo.UnlockAchievement && !AchievementSaveSystem.instance.IsAchievementUnlocked(skinSo.UnlockAchievement.AchievementID))
            {
                skinImage.color = Color.black;
                GetComponent<Image>().color = disabledColor;
            }
            else
            {
                skinImage.color = Color.white;
                GetComponent<Image>().color = standardImageColor;
            }
        }
    }

    public void ResetVisuals()
    {
        gameObject.GetComponent<Outline>().effectColor = new Color(0, 0, 0, 0);
        transform.localScale = originalScale;
        skinImage.color = Color.white;
        GetComponent<Image>().color = standardImageColor;

        foreach (Image image in selectionimages)
        {
            image.enabled = false;
            image.color = Color.white;
        }

        foreach (TextMeshProUGUI text in selectionTexts)
            text.enabled = false;

        activePlayers.Clear();
        hoveredAmount = 0;

        for (int i = 0; i < isHovering.Length; i++)
            isHovering[i] = false;
    }

    public void PerformUnlockAnimation()
    {
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        if (targetTransform == null && skinImage != null)
        {
            targetTransform = skinImage.rectTransform;
            baseLocalScale = targetTransform.localScale;
        }

        fadeCoroutine = StartCoroutine(AnimateUnlockSequence());
    }

    private IEnumerator AnimateUnlockSequence()
    {
        GetComponent<StudioEventEmitter>().Play();
        Vector3 startScale = baseLocalScale * startScaleMultiplier;
        Vector3 peakScale = baseLocalScale * peakScaleMultiplier;

        skinImage.color = Color.black;
        Image buttonBg = GetComponent<Image>();
        if (buttonBg != null)
        {
            buttonBg.color = disabledColor;
        }

        targetTransform.localScale = startScale;
        targetTransform.localRotation = Quaternion.Euler(0, 0, startRotation);

        float elapsedTime = 0f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float rawProgress = Mathf.Clamp01(elapsedTime / fadeDuration);

            float easedProgress = colorEase.Evaluate(rawProgress);

            skinImage.color = Color.Lerp(Color.black, Color.white, easedProgress);
            if (buttonBg != null)
            {
                buttonBg.color = Color.Lerp(disabledColor, standardImageColor, easedProgress);
            }

            if (rawProgress < 0.7f)
            {
                float popProgress = rawProgress / 0.7f;
                targetTransform.localScale = Vector3.Lerp(startScale, peakScale, Mathf.Sin(popProgress * Mathf.PI * 0.5f));
            }
            else
            {
                float settleProgress = (rawProgress - 0.7f) / 0.3f;
                targetTransform.localScale = Vector3.Lerp(peakScale, baseLocalScale, settleProgress);
            }

            targetTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(startRotation, 0f, easedProgress));

            yield return null;
        }

        skinImage.color = Color.white;
        if (buttonBg != null)
        {
            buttonBg.color = standardImageColor;
        }
        targetTransform.localScale = baseLocalScale;
        targetTransform.localRotation = Quaternion.identity;

        fadeCoroutine = null;
    }

    public void ActDisabled()
    {
        skinImage.color = Color.black;
        GetComponent<Image>().color = disabledColor;
    }
}