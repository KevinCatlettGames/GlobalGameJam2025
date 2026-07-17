using Febucci.UI;
using System.Collections;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : NetworkBehaviour
{
    [Header("Spells")]
    [SerializeField] private Image firstSpellImage;
    [SerializeField] private Image firstCoverImage;
    [SerializeField] private Image secondSpellImage;
    [SerializeField] private Image secondCoverImage;

    private float firstCoverFill = 0f;
    private float firstCDRate = 1f;
    private float secondCoverFill = 0f;
    private float secondCDRate = 1f;
    private Sprite[] spellSprites = new Sprite[4];
    private Sprite[] portraitSprites;
    private int currentPortraitIndex = -1;

    [Header("Animation")]
    [SerializeField] private AnimationCurve shakeCurve;
    [SerializeField] private float shakeTime = .1f;
    [SerializeField] private float shakeAmplitude = 2f;
    [SerializeField] private RectTransform firstSpellTransform;
    [SerializeField] private RectTransform secondSpellTransform;
    [SerializeField] private Animator highDamageIndicator;
    [SerializeField] private float highDamageThreshold = 100f;
    private Coroutine firstSpellShake;
    private Coroutine secondSpellShake;

    [Header("Damage")]
    [SerializeField] private TypewriterByWord damageTypewriter;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private float gradientEvaluateFactor = 0.005f;
    [SerializeField] private Gradient damageTextColorGradient;

    [Header("UI Elements")]
    [SerializeField] private Image portrait;
    [SerializeField] private Color deathColor;
    [SerializeField] private SkinSO skin;
    public SkinSO Skin { get { return skin; } }
    [SerializeField] private GameObject UICover;
    [SerializeField] private Image[] coloredUI;
    [SerializeField] private Slider ultSlider;

    [Header("Score")]
    [SerializeField] private TypewriterByWord lifesTypewriter;

    [Header("GameSettings")]
    [SerializeField] private SO_GameSettings gameSettings;

    private int lifes = 0;
    private int maxLifes = 0;

    private void Start()
    {
        firstCoverImage.fillAmount = firstCoverFill;
        secondCoverImage.fillAmount = secondCoverFill;
        if (gameSettings != null)
        {
            maxLifes = gameSettings.Lifes;
            if (maxLifes != -1 && maxLifes > 1)
            {
                lifes = maxLifes;
                lifesTypewriter.gameObject.SetActive(true);
                lifesTypewriter.ShowText("x"+ lifes.ToString());
                GameManager.Instance.OnGameStarted += ResetLifes;
            }
        }
    }

    private void Update()
    {
        if (firstCoverFill > 0f)
        {
            firstCoverFill -= firstCDRate * Time.deltaTime;
            if (firstCoverFill <= 0)
            {
                firstCoverFill = 0;
            }
            firstCoverImage.fillAmount = firstCoverFill;
        }

        if (secondCoverFill > 0f)
        {
            secondCoverFill -= secondCDRate * Time.deltaTime;
            if (secondCoverFill <= 0)
            {
                secondCoverFill = 0;
            }
            secondCoverImage.fillAmount = secondCoverFill;
        }
    }
    public void InitialisePlayerHUD(int playerID)
    {
        skin = LobbyPlayerValues.Instance.playerValuesList[playerID].Skin;

        if (LobbyManager.instance && LobbyManager.instance.SelectedGameMode == GameManager.GameModeType.Team)
        {
            if(LobbyPlayerValues.Instance.playerValuesList[playerID].TeamIndex == 1)
            {
                foreach (var uiElement in coloredUI)
                    uiElement.color = LobbyManager.instance.TeamColors[0];
            }
            else if (LobbyPlayerValues.Instance.playerValuesList[playerID].TeamIndex == 2)
            {
                foreach (var uiElement in coloredUI)
                    uiElement.color = LobbyManager.instance.TeamColors[1];
            }
        }
        else
        {
            foreach (var uiElement in coloredUI)
                uiElement.color = skin.Color;
        }

        portraitSprites = skin.HeadSprites;
        SetPortrait(0);
    }
    public void InitialisePlayerHUD(SkinSO skin)
    {
        foreach (var uiElement in coloredUI)
        {
            uiElement.color = skin.Color;
        }
        portraitSprites = skin.HeadSprites;
        SetPortrait(0);
    }

    public void SetSpell(int spellID, Sprite spellImage, Sprite spellUsedImage)
    {
        switch (spellID)
        {
            case 1:
                spellSprites[1] = spellImage;
                spellSprites[0] = spellUsedImage;
                firstSpellImage.sprite = spellImage;
                firstCoverFill = 0f;
                firstCoverImage.fillAmount = firstCoverFill;
                break;
            case 2:
                spellSprites[2] = spellImage;
                spellSprites[3] = spellUsedImage;
                secondSpellImage.sprite = spellImage;
                secondCoverFill = 0f;
                secondCoverImage.fillAmount = secondCoverFill;
                break;
            default:
                Debug.LogWarning($"SetSpell called with invalid spellID: {spellID}");
                break;
        }
    }
    public void SetSpellCooldown(int spellID, float cooldownRate)
    {
        switch (spellID)
        {
            case 1:
                firstCoverFill = 1f;
                firstCoverImage.fillAmount = firstCoverFill;
                firstCDRate = cooldownRate;
                break;
            case 2:
                secondCoverFill = 1f;
                secondCoverImage.fillAmount = secondCoverFill;
                secondCDRate = cooldownRate;
                break;
            default:
                Debug.LogWarning($"SetSpellCooldown called with invalid spellID: {spellID}");
                break;
        }
    }
    public void AnimateSpellIcon(int spellID)
    {
        switch (spellID)
        {
            case 1:
                if (firstSpellShake == null)
                {
                    firstSpellShake = StartCoroutine(shakeSpellCoroutine(firstSpellTransform, spellID));
                }
                break;
            case 2:
                if (secondSpellShake == null)
                {
                    secondSpellShake = StartCoroutine(shakeSpellCoroutine(secondSpellTransform, spellID));
                }
                break;
            default:
                Debug.LogWarning($"AnimateSpellIcon called with invalid spellID: {spellID}");
                break;
        }
    }
    private IEnumerator shakeSpellCoroutine(RectTransform spellTransform, int spellID)
    {
        Vector3 originalPosition = spellTransform.position;
        float progress = 0;
        float progression = 1 / shakeTime;
        int spriteIndex = spellID == 1 ? 0 : 3;
        Image spellImage = spellID == 1 ? firstSpellImage : secondSpellImage;
        spellImage.sprite = spellSprites[spriteIndex];
        while (progress < 1)
        {
            spellTransform.position = originalPosition + Vector3.right * shakeCurve.Evaluate(progress) * shakeAmplitude;
            progress += progression * Time.deltaTime;
            yield return null;
        }
        spellTransform.position = originalPosition;
        switch (spellID)
        {
            case 1:
                firstSpellImage.sprite = spellSprites[1];
                firstSpellShake = null;
                break;
            case 2:
                secondSpellImage.sprite = spellSprites[2];
                secondSpellShake = null;
                break;
            default:
                Debug.LogWarning($"Spell shake ID Issue, spellID: {spellID}");
                break;
        }
    }

    public void UpdateDamageText(int damage)
    {
        if (damageText != null)
        {
            if(damageTypewriter.enabled)
                damageTypewriter.ShowText(damage.ToString());
            else
                damageText.text = damage.ToString();

            float colorValue = damage * gradientEvaluateFactor;
            damageText.color = damageTextColorGradient.Evaluate(colorValue);
            damageTypewriter.enabled = true;
        }
        if (damage >= highDamageThreshold && currentPortraitIndex != 2)
        {
            SetPortrait(1);
            highDamageIndicator.SetBool("hasHighDamage", true);
        }
    }

    public void DisplayDeath()
    {
        SetPortrait(2);
        UICover.SetActive(true);
        highDamageIndicator.SetBool("hasHighDamage", false);
        if (maxLifes != -1 && maxLifes > 1)
        {
            lifes--;
            lifesTypewriter.ShowText("x" + lifes.ToString());
        }
    }

    private void ResetLifes()
    {
        if (maxLifes == -1) return;
        lifes = maxLifes;
        lifesTypewriter.ShowText("x" + lifes.ToString());
    }

    public void ResetHUD()
    {
        portrait.color = Color.white;
        UICover.SetActive(false);
        damageTypewriter.enabled = false;
        UpdateDamageText(0);
        SetPortrait(0);
        ChargeUlt(false);
        SetUltSlider(0);
        highDamageIndicator.SetBool("hasHighDamage", false);
    }

    private void SetPortrait(int portaritIndex)
    {
        if (portaritIndex == currentPortraitIndex || portaritIndex < 0 || portaritIndex >= portraitSprites.Length) return;
        currentPortraitIndex = portaritIndex;
        portrait.sprite = portraitSprites[currentPortraitIndex];
    }

    public void SetUltSlider(float value)
    {
        return; // Remove when Ult back
        value = Mathf.Clamp01(value);
        ultSlider.value = value;
    }

    public void ChargeUlt(bool isCharged)
    {
        Color color = isCharged ? Color.yellow : Color.white;
        firstSpellImage.color = color;
        secondSpellImage.color = color;
    }

    private void OnDestroy()
    {
        if (maxLifes != -1)
        {
            GameManager.Instance.OnGameStarted -= ResetLifes;
        }
    }
}
