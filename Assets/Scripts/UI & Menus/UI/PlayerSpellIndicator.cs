using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

public class PlayerSpellIndicator : MonoBehaviour
{
    [SerializeField] private Image spellDot;
    [SerializeField] private Image spellCover;
    private float rate = .1f;
    void Update()
    {
        if (spellCover.fillAmount > 0) 
        {
            spellCover.fillAmount -= rate * Time.deltaTime;
        }
    }
    public void SetSpellCooldown(float rate)
    {
        this.rate = rate;
        spellCover.fillAmount = 1;
    }
    public void SetNewSpellColor(Color color)
    {
        spellDot.color = color;
        spellCover.fillAmount = 0;
    }
}
