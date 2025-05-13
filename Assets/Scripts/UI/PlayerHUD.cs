using Febucci.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [Header("Spells")]
    [SerializeField] private Image firstSpellImage;
    [SerializeField] private Image firstCoverImage;
    [SerializeField] private Image secondSpellImage;
    [SerializeField] private Image secondCoverImage;
    private float firstCoverFill = 0f;
    private float firsCDRate = 1f;
    private float secondCoverFill = 0f;
    private float secondCDRate = 1f;
    [Header("Damage")]
    [SerializeField] private TypewriterByWord damageTypewriter;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private float gradientEvalueateFactor = 0.005f;
    [SerializeField] private Gradient damageTextColorGradient;
    [Header("UI Elements")]
    [SerializeField] private Image portrait;
    [SerializeField] Color deathColor;
    [SerializeField] private GameObject UICover;
    [SerializeField] private Image[] coloredUI;
    [Header("Score")]
    [SerializeField] private TypewriterByWord winsTypewriter;
    [SerializeField] private TypewriterByWord killsTypewriter;

    private int kills = 0;
    private int wins = 0;

    private void Start()
    {
        firstCoverImage.fillAmount = firstCoverFill;
        secondCoverImage.fillAmount = secondCoverFill;
        killsTypewriter.ShowText(kills.ToString());
        winsTypewriter.ShowText(wins.ToString());
    }
    private void Update()
    {
        if (firstCoverFill > 0)
        {
            firstCoverFill -= firsCDRate * Time.deltaTime;
            if (firstCoverFill < 0) firstCoverFill = 0;
            firstCoverImage.fillAmount = firstCoverFill;
        }
        if (secondCoverFill > 0) 
        {
            secondCoverFill -= secondCDRate * Time.deltaTime;
            if(secondCoverFill < 0) secondCoverFill = 0;
            secondCoverImage.fillAmount = secondCoverFill;
        }
    }
    public void SetSpell(int spellID, Sprite spellImage)
    {
        switch (spellID)
        {
            case 1:
                firstSpellImage.sprite = spellImage;
                firstCoverFill = 0f;
                firstCoverImage.fillAmount = firstCoverFill;
                break;
            case 2:
                secondSpellImage.sprite = spellImage;
                secondCoverFill = 0f;
                secondCoverImage.fillAmount = secondCoverFill;
                break;
            default:
                return;
        }
    }

    public void UpdateDamageText(int damage)
    {
        if (damageText != null)
        {
            damageTypewriter.ShowText(damage.ToString());
            //damageText.text = damage.ToString();
            float colorValue = damage * gradientEvalueateFactor;
            damageText.color = damageTextColorGradient.Evaluate(colorValue);
        }
    }
    
    public void SetSpellCooldown(int spellID, float cooldownRate)
    {
        switch (spellID)
        {
            case 1:
                firstCoverFill = 1f;
                firstCoverImage.fillAmount = firstCoverFill;
                firsCDRate = cooldownRate;
                break;
            case 2:
                secondCoverFill = 1f;
                secondCoverImage.fillAmount = secondCoverFill;
                secondCDRate = cooldownRate;
                break;
            default:
                return;
        }
    }

    public void AddWin()
    {
        wins++;
        winsTypewriter.ShowText(wins.ToString());
    }
    public void AddKill()
    {
        kills++;
        killsTypewriter.ShowText(kills.ToString());
    }
    public void DisplayDeath()
    {
        portrait.color = deathColor;
        UICover.SetActive(true);    
    }
    public void ResetHUD()
    {
        portrait.color = Color.white;
        UICover.SetActive(false);
        UpdateDamageText(0);
    }
    public void InitialisePlayerHUD(Color playerColor, Sprite playerPortrait)
    {
        for (int i = 0; i < coloredUI.Length; i++)
        {
            coloredUI[i].color = playerColor;
        }
        portrait.sprite = playerPortrait;
    }
}
