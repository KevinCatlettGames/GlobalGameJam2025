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
    [Tooltip("The RectTransform that gets animated during the unlock pop (defaults to skinImage if left empty).")]
    [SerializeField] private RectTransform targetTransform;

    [Tooltip("Optional GameObject for a 'New Unlock' star/badge sprite that turns on during the reveal phase.")]
    [SerializeField] private GameObject unlockStarBadge;

    [Header("Animation Settings")]
    [Tooltip("Duration of the scale-up phase while remaining silhouetted (Black).")]
    [SerializeField] private float scaleUpDuration = 0.35f;

    [Tooltip("Duration to hold at peak scale while revealed (White) before scaling back down.")]
    [SerializeField] private float revealHoldDuration = 0.5f;

    [Tooltip("Duration of the scale-down settle phase back to base size.")]
    [SerializeField] private float scaleDownDuration = 0.25f;

    [Tooltip("Easing curve controlling background color transition and rotation snap/smoothing over time.")]
    [SerializeField] private AnimationCurve colorEase = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Impact Juice (Relative Scale Multipliers)")]
    [Tooltip("Starting scale multiplier relative to base scale when the animation starts (e.g., 0.5 = 50% size).")]
    [SerializeField] private float startScaleMultiplier = 0.5f;

    [Tooltip("Maximum scale multiplier reached during the pop phase (e.g., 1.25 = 125% size).")]
    [SerializeField] private float peakScaleMultiplier = 1.25f;

    [Tooltip("Initial Z-rotation offset in degrees when the animation begins (e.g., -15 degrees tilt).")]
    [SerializeField] private float startRotation = -15f;

    // Cached initial scale to preserve aspect ratio & UI layout
    private Vector3 baseLocalScale;
    private Vector3 starBaseScale = Vector3.one;
    private RectTransform starTransform;

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

        if (unlockStarBadge != null)
        {
            starTransform = unlockStarBadge.GetComponent<RectTransform>();
            if (starTransform != null)
            {
                starBaseScale = starTransform.localScale;
            }
            unlockStarBadge.SetActive(false);
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

        if (unlockStarBadge != null)
        {
            if (starTransform != null) starTransform.localScale = starBaseScale;
            unlockStarBadge.SetActive(false);
        }

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

        // Phase 0: Lock initial silhouette state
        skinImage.color = Color.black;
        Image buttonBg = GetComponent<Image>();
        if (buttonBg != null)
        {
            buttonBg.color = disabledColor;
        }

        targetTransform.localScale = startScale;
        targetTransform.localRotation = Quaternion.Euler(0, 0, startRotation);

        if (unlockStarBadge != null)
        {
            if (starTransform != null) starTransform.localScale = starBaseScale;
            unlockStarBadge.GetComponent<Image>().color = standardImageColor;
            unlockStarBadge.SetActive(true);
        }

        // =========================================================================
        // PHASE 1: Scale up to peak as a black silhouette
        // =========================================================================
        float t = 0f;
        while (t < scaleUpDuration)
        {
            t += Time.deltaTime;
            float p = scaleUpDuration > 0f ? Mathf.Clamp01(t / scaleUpDuration) : 1f;

            targetTransform.localScale = Vector3.Lerp(startScale, peakScale, Mathf.Sin(p * Mathf.PI * 0.5f));
            targetTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Lerp(startRotation, 0f, p));

            yield return null;
        }

        // Snap to exact peak transform state
        targetTransform.localScale = peakScale;
        targetTransform.localRotation = Quaternion.identity;

        // =========================================================================
        // REVEAL MOMENT: Switch to full color & pop open star badge
        // =========================================================================
        skinImage.color = Color.white;
        if (buttonBg != null)
        {
            buttonBg.color = standardImageColor;
        }

        // =========================================================================
        // PHASE 2: Hold at peak scale for the reveal moment
        // =========================================================================
        if (revealHoldDuration > 0f)
        {
            yield return new WaitForSeconds(revealHoldDuration);
        }

        // =========================================================================
        // PHASE 3: Settle skin scale down & shrink star down to 0 before disabling
        // =========================================================================
        t = 0f;
        while (t < scaleDownDuration)
        {
            t += Time.deltaTime;
            float p = scaleDownDuration > 0f ? Mathf.Clamp01(t / scaleDownDuration) : 1f;

            // Scale down skin target transform
            targetTransform.localScale = Vector3.Lerp(peakScale, baseLocalScale, p);

            // Scale down star badge transform concurrently
            if (starTransform != null && unlockStarBadge.activeSelf)
            {
                starTransform.localScale = Vector3.Lerp(starBaseScale, Vector3.zero, p);
            }

            yield return null;
        }

        // Ensure final baseline integrity & clean up star state
        skinImage.color = Color.white;
        if (buttonBg != null)
        {
            buttonBg.color = standardImageColor;
        }
        targetTransform.localScale = baseLocalScale;
        targetTransform.localRotation = Quaternion.identity;

        if (unlockStarBadge != null)
        {
            unlockStarBadge.SetActive(false);
            if (starTransform != null) starTransform.localScale = starBaseScale; // Reset base scale for future use
        }

        fadeCoroutine = null;
    }

    public void ActDisabled()
    {
        skinImage.color = Color.black;
        GetComponent<Image>().color = disabledColor;
    }
}