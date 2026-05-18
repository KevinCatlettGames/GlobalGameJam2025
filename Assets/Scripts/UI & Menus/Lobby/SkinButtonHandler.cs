using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI; 

public class SkinButtonHandler : MonoBehaviour
{
    public SkinButtonHandler rightSkinSelection;
    public SkinButtonHandler leftSkinSelection;
    public SkinButtonHandler topSkinSelection;
    public SkinButtonHandler bottomSkinSelection;
    public bool isSelected = false;
    public Image selectionimage;
    public TextMeshProUGUI selectionText;
    public SkinSO skinSo;
    public Image skinImage;
    public Image shineImage;
    public Color standardImageColor = Color.gray;
    public Color disabledColor = Color.red; 
    public Vector3 originalScale;
    public float scaleMultiplier;

    private void Awake()
    {
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {      
        if (!SteamIntegration.instance.IsFullVersion && !skinSo.AvailableInDemo)
            GetComponent<Image>().color = disabledColor;
        else
            GetComponent<Image>().color = standardImageColor;
    }

    public void TogglePlayerIcon(bool activate, int playerIndex)
    {
        selectionimage.enabled = activate;
        selectionimage.color = skinSo.Color;
        if (activate)
        {
            gameObject.GetComponent<Outline>().effectColor = skinSo.Color;
            transform.localScale = originalScale * scaleMultiplier;
        }
        else
        {
            gameObject.GetComponent<Outline>().effectColor = new Color(0, 0, 0, 0);
            transform.localScale = originalScale;
        }

        selectionText.enabled = activate;
        selectionText.text = "P" + (playerIndex+1);
    }

    public void ToggleReadyVisuals()
    {

        if (GetComponent<Image>().color == skinSo.Color)
        {
            GetComponent<Image>().color = standardImageColor;
            selectionimage.gameObject.SetActive(true);
            shineImage.enabled = false;
        }
        else
        {
            GetComponent<Image>().color = skinSo.Color;
            selectionimage.gameObject.SetActive(false);
            shineImage.enabled = true;
        }
    }

    public void ResetVisuals()
    {
        gameObject.GetComponent<Outline>().effectColor = new Color(0, 0, 0, 0);
        transform.localScale = originalScale;
        GetComponent<Image>().color = standardImageColor;
        selectionimage.enabled = false;
        selectionimage.color = Color.white;
        selectionText.enabled = false;
        shineImage.enabled = false;
        isSelected = false;
    }
}