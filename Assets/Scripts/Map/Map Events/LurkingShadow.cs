using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LurkingShadow : MonoBehaviour
{
    private SpriteRenderer spriteRenderer;
    private float currentAlpha = 0f;
    private Color color;
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();    
        color = spriteRenderer.color;
        spriteRenderer.color = new Color(color.r, color.g, color.b, 0);
    }
    public void LerpShadow(float targetValue, float time)
    {
        Debug.Log("Lerp");
        StopAllCoroutines();
        if (time <= 0f)
        {
            spriteRenderer.color = color;
            return;
        }
        if (targetValue < currentAlpha)
        {
            StartCoroutine(DecreaseShadow(targetValue, time));
        }
        else if (targetValue > currentAlpha) 
        {
            StartCoroutine(IncreaseShadow(targetValue, time));
        }
    }
    private IEnumerator DecreaseShadow(float target, float time)
    {
        float timer = 0;
        float shadowDecrease = (currentAlpha - target) / time;
        while (timer < time)
        {
            currentAlpha -= shadowDecrease * Time.deltaTime;
            if (currentAlpha < 0) 
                currentAlpha = 0;           
            spriteRenderer.color = new Color(color.r, color.g, color.b, currentAlpha);
            yield return null;
        }
    }
    private IEnumerator IncreaseShadow(float target, float time)
    {
        float timer = 0;
        float shadowIncrease = (target - currentAlpha) / time;
        while (timer < time)
        {
            currentAlpha += shadowIncrease * Time.deltaTime;
            if (currentAlpha > 1)
                currentAlpha = 1;
            spriteRenderer.color = new Color(color.r, color.g, color.b, currentAlpha);
            yield return null;
        }
    }
}
