using Febucci.UI;
using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TypewriterByWord damageTypewriter;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private float gradientEvaluateFactor = 0.005f;
    [SerializeField] private Gradient damageTextColorGradient;

    public void InitialiseDamagePopup(int damage)
    {
        float colorValue = (float)damage * gradientEvaluateFactor;
        damageText.color = damageTextColorGradient.Evaluate(colorValue);
        damageTypewriter.ShowText(damage.ToString());
    }

    public void Dissapear()
    {
        Destroy(gameObject);
    }

    public void DissapearText()
    {
        damageTypewriter.StartDisappearingText();
    }
}
