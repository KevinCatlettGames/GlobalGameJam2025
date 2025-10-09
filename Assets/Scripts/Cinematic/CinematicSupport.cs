using UnityEngine;
using UnityEngine.UI;

public class CinematicSupport : MonoBehaviour
{
    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeInSpeed = 1f;
    [SerializeField] private float fadeOutSpeed = 1f;
    [SerializeField] private float holdDuration = 2f; 

    private enum FadeState { Idle, FadingIn, Holding, FadingOut }
    private FadeState fadeState = FadeState.Idle;

    private float holdTimer = 0f;

    private void Update()
    {
        switch (fadeState)
        {
            case FadeState.FadingIn:
                FadeTo(1f, fadeInSpeed); 
                if (Mathf.Approximately(fadeImage.color.a, 1f))
                {
                    fadeState = FadeState.Holding;
                    holdTimer = holdDuration;
                }
                break;

            case FadeState.Holding:
                holdTimer -= Time.deltaTime;
                if (holdTimer <= 0f)
                {
                    fadeState = FadeState.FadingOut;
                }
                break;

            case FadeState.FadingOut:
                FadeTo(0f, fadeOutSpeed); 
                if (Mathf.Approximately(fadeImage.color.a, 0f))
                {
                    fadeState = FadeState.Idle;
                    EndAnim(); 
                }

                if (GetComponent<Camera>().enabled)
                    GetComponent<Camera>().enabled = false;
                break;

            case FadeState.Idle:
                break;
        }
    }

    private void FadeTo(float targetAlpha, float speed)
    {
        Color color = fadeImage.color;
        color.a = Mathf.MoveTowards(color.a, targetAlpha, speed * Time.deltaTime);
        fadeImage.color = color;
    }

    public void TriggerFade()
    {
        fadeState = FadeState.FadingIn;
    }

    public void EndAnim()
    {
        CameraHandler.Instance?.InvokeCinematicEnd();
    }
}