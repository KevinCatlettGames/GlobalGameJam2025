using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private float gradientEvalueateFactor = 0.005f;
    [SerializeField] private Gradient damageTextColorGradient;
    [Header("Portrait")]
    [SerializeField] private Image portrait;
    [SerializeField] Color deathColor;
    [SerializeField] private GameObject UICover;
    [Header("Score")]
    [SerializeField] private TextMeshProUGUI scoreText;
    private int score = 0;

    private void Start()
    {
        firstCoverImage.fillAmount = firstCoverFill;
        secondCoverImage.fillAmount = secondCoverFill;
        scoreText.text = "K: " + score.ToString();
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
            damageText.text = damage.ToString();
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

    public void SetScore(int score)
    {
        this.score = score;
        scoreText.text = "K: " + score.ToString();
    }
    public void AddScore()
    {
        score++;
        scoreText.text = "K: " + score.ToString();
    }
    public void DisplayDeath()
    {
        portrait.color = deathColor;
        UICover.SetActive(true);    
    }
    public void Reset()
    {
        portrait.color = Color.white;
        UICover.SetActive(false);
    }
}
