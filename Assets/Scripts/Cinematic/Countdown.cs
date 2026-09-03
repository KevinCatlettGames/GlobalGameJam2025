using FMOD.Studio;
using FMODUnity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class Countdown : MonoBehaviour
{
    [Header("Countdown Settings")]
    [SerializeField] private float timeBetweenElements = .5f;
    [SerializeField] EventReference countDownEvent;

    [Header("Sprite Countdown")]
    [SerializeField] private Image countdownImage;
    [SerializeField] private Sprite[] countdownSprites;
    //[SerializeField] private StudioEventEmitter[] countdownEmitters;
    //[SerializeField] private StudioEventEmitter goEmitter;

    [Header("Events")]
    public UnityEvent OnCountdownStart;
    public UnityEvent onCountdownComplete;

    private Coroutine countdownCoroutine;
    private Animation animation;

    private void Start()
    {
        CameraHandler.Instance.onCinematicEnd.AddListener(StartCountdown);

        if (countdownImage != null)
            countdownImage.enabled = false;

        animation = GetComponent<Animation>();
        GameManager.Instance.OnGameStarted += StartShortCountdown;
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
    public void StartShortCountdown()
    {
        StartCoroutine(ShortCountdown());
    }
    private IEnumerator ShortCountdown()
    {
        yield return new WaitForSeconds(timeBetweenElements / 2);
        countdownImage.enabled = true;
        countdownImage.sprite = countdownSprites[0];
        animation.Play();
        yield return new WaitForSeconds(timeBetweenElements * 2);
        countdownImage.enabled = false;
        PlayerManager.Instance.EnablePlayerInput();
    }

    private IEnumerator CountdownRoutine()
    {
        bool soundStarted = false;
        int currentCount = countdownSprites.Length -1;
        yield return new WaitForSeconds(.1f);
        List<PlayerController> players = PlayerManager.Instance.GetPlayers();
        int playerCount = players.Count -1;
        float animTime = .45f;

        while (currentCount > -1)
        {
            yield return new WaitForSeconds(timeBetweenElements - animTime);
            if (TransportSwitcher.Instance && TransportSwitcher.Instance.isUsingRelay)
            {
                if (currentCount <= playerCount)
                {
                    players[currentCount].StartEntrence(false);
                }
            }
            yield return new WaitForSeconds(animTime);
            if (!soundStarted)
            {
                soundStarted = true;
                EventInstance fmodEvent = RuntimeManager.CreateInstance(countDownEvent);
                RuntimeManager.AttachInstanceToGameObject(fmodEvent, transform, GetComponent<Rigidbody>());
                int r = Random.Range(0, 5); //Amount of voice profiles
                fmodEvent.setParameterByName("VoiceProfile", r);
                fmodEvent.start();
                fmodEvent.release();
            }
            countdownImage.enabled = true;
            countdownImage.sprite = countdownSprites[currentCount];
            currentCount--;
        }
        animation.Play();
        yield return new WaitForSeconds(timeBetweenElements);
        onCountdownComplete?.Invoke();
        PlayerManager.Instance.EnablePlayerInput();
        countdownImage.enabled = false;
    }

    private void OnDestroy()
    {
        GameManager.Instance.OnGameStarted -= StartShortCountdown;
    }
}