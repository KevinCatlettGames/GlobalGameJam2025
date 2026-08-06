using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using FMODUnity;
using System.Collections.Generic;

public class Countdown : MonoBehaviour
{
    [Header("Countdown Settings")]
    [SerializeField] private float timeBetweenElements = .5f;

    [Header("Sprite Countdown")]
    [SerializeField] private Image countdownImage;
    [SerializeField] private Sprite[] countdownSprites;
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
        int currentCount = countdownSprites.Length -1;
        yield return new WaitForSeconds(.1f);

        List<PlayerController> players = PlayerManager.Instance.GetPlayers();
        int playerCount = players.Count -1;

        while (currentCount > -1)
        {
            yield return new WaitForSeconds(timeBetweenElements - 0.4f);
            if (currentCount <= playerCount)
            {
                players[currentCount].StartEntrence(currentCount * timeBetweenElements);
            }
            yield return new WaitForSeconds(0.4f);
            countdownImage.enabled = true;
            countdownImage.sprite = countdownSprites[currentCount];
            currentCount--;
        }
        yield return new WaitForSeconds(timeBetweenElements);
        onCountdownComplete?.Invoke();
        countdownImage.enabled = false;
    }
}