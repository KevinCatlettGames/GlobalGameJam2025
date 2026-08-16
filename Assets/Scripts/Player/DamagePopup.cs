using Febucci.UI;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DamagePopup : MonoBehaviour
{
    [SerializeField] private TypewriterByWord damageTypewriter;
    [SerializeField] private TextMeshProUGUI damageText;
    [SerializeField] private Image critImage;
    [SerializeField] private float gradientEvaluateFactor = 0.005f;
    [SerializeField] private Gradient damageTextColorGradient;
    [SerializeField] private Gradient critTextColorGradient;
    [SerializeField] private Gradient critImageColorGradient;
    [SerializeField] private float damageRandomOffset = .5f;
    [SerializeField] private float yOffset = 1f;

    public void InitialiseDamagePopup(int damage, bool isCrit)
    {
        float colorValue = (float)damage * gradientEvaluateFactor;
        damageTypewriter.ShowText(damage.ToString());
        if(critImage && isCrit)
        {
            critImage.enabled = true;
            damageText.color = critTextColorGradient.Evaluate(colorValue);
            critImage.color = critImageColorGradient.Evaluate(colorValue);
        }
        else
        {
            damageText.color = damageTextColorGradient.Evaluate(colorValue);
        }
        Vector2 r = Random.insideUnitCircle;
        Vector3 offset = new Vector3(r.x, yOffset, r.y) * damageRandomOffset;
        transform.position += offset;

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
