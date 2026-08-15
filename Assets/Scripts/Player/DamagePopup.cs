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
    [SerializeField] private float damageRandomOffset = .5f;
    [SerializeField] private float yOffset = 1f;

    public void InitialiseDamagePopup(int damage, bool isCrit)
    {
        Debug.Log("IcCrit " + isCrit);
        float colorValue = (float)damage * gradientEvaluateFactor;
        damageText.color = damageTextColorGradient.Evaluate(colorValue);
        damageTypewriter.ShowText(damage.ToString());
        if(critImage)
            critImage.enabled = isCrit;
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
