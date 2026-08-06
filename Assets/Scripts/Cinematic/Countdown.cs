using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using FMODUnity; 

public class Countdown : MonoBehaviour
{
    [Header("Countdown Settings")]
    public int countdownTime = 3;
    [SerializeField] private float timeBetweenElements = .5f;

    [Header("Sprite Countdown")]
    [SerializeField] private Image countdownImage;
    [SerializeField] private Sprite[] countdownSprites;
    [SerializeField] private Sprite goSprite;
    //[SerializeField] private StudioEventEmitter[] countdownEmitters;
    //[SerializeField] private StudioEventEmitter goEmitter;

    [Header("Events")]
    public UnityEvent OnCountdownStart;
    public UnityEvent onCountdownComplete;

    private Coroutine countdownCoroutine;

    private void Start()
    {
        CameraHandler.Instance.onCinematicEnd.AddListener(StartCountdown);

        if (countdownImage != null)
            countdownImage.enabled = false;
    }

    public void StartCountdown()
    {  
        CameraHandler.Instance.onCinematicEnd.RemoveListener(StartCountdown);
        if (CameraHandler.Instance.playCinematicAtStart)
        {
            OnCountdownStart?.Invoke();

            if (countdownCoroutine != null)
                StopCoroutine(countdownCoroutine);

            countdownCoroutine = StartCoroutine(CountdownRoutine());
        }
        else
        {
            onCountdownComplete?.Invoke();
        }
    }

    private IEnumerator CountdownRoutine()
    {
        int currentCount = countdownTime;
        yield return new WaitForSeconds(1.2f); //Delay for entry animations

        countdownImage.enabled = true;

        while (currentCount > 0)
        {
            int spriteIndex = currentCount - 1;

            if (spriteIndex >= 0 && spriteIndex < countdownSprites.Length)
            {
                countdownImage.sprite = countdownSprites[spriteIndex];
            }

            yield return new WaitForSeconds(timeBetweenElements);
            currentCount--;
        }

        if (goSprite != null)
        {
            countdownImage.sprite = goSprite;
        }

        yield return new WaitForSeconds(timeBetweenElements);

        onCountdownComplete?.Invoke();
        countdownImage.enabled = false;
    }
}