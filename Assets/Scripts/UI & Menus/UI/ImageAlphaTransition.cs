using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Image))]
public class ImageAlphaTransition : MonoBehaviour
{
    private Image image;
    [SerializeField] private float transitionTime = 1f;

    private Coroutine currentTransition;

    private void Start()
    {
        image = GetComponent<Image>();

        if (SceneTransition.instance)
        {
            SceneTransition.instance.OnSceneTransition.AddListener(Transition);
            SceneTransition.instance.OnMenuTransition.AddListener(MainMenuTransition);
        }
    }

    private void OnDisable()
    {
        if (SceneTransition.instance)
            SceneTransition.instance.OnSceneTransition.RemoveListener(Transition);
    }

    private void Transition(bool fadeIn)
    {
        if (currentTransition != null)
            StopCoroutine(currentTransition);

        currentTransition = StartCoroutine(FadeRoutine(fadeIn));
    }

    private void MainMenuTransition()
    {
        if (currentTransition != null)
            StopCoroutine(currentTransition);

        currentTransition = StartCoroutine(FadeRoutine(false));
    }

    private IEnumerator FadeRoutine(bool fadeIn)
    {
        image.enabled = true;
        
        float timer = 0f;

        float startAlpha = fadeIn ? 1f : 0f;
        float endAlpha = fadeIn ? 0f : 1f;

        Color color = image.color;
        color.a = startAlpha;
        image.color = color;

        while (timer < transitionTime)
        {
            timer += Time.deltaTime;
            float t = timer / transitionTime;

            color.a = Mathf.Lerp(startAlpha, endAlpha, t);
            image.color = color;

            yield return null;
        }
        
        if(fadeIn)
            image.enabled = false;
            
        if(SceneTransition.instance) 
            SceneTransition.instance.InvokeOnTransitionFinished();
    }
}