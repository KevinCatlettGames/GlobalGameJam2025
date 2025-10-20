using System.Collections;
using UnityEngine;
using TMPro;
using Unity.Netcode;
using UnityEngine.Events;

public class Countdown : MonoBehaviour
{
    [Header("Countdown Settings")]
    public int countdownTime = 3;
    [SerializeField] private float timeBetweenElements = .5f;
    public TextMeshProUGUI countdownText;

    [Header("Events")]
    public UnityEvent OnCountdownStart;
    public UnityEvent onCountdownComplete;

    private Coroutine countdownCoroutine;
    public string countDownEndText = "GO!";
    private void Start()
    {
        CameraHandler.Instance.onCinematicEnd.AddListener(StartCountdown);
    }

    public void StartCountdown()
    {
        CameraHandler.Instance.onCinematicEnd.RemoveListener(StartCountdown);
        OnCountdownStart?.Invoke();

        if (countdownCoroutine != null)
            StopCoroutine(countdownCoroutine);

        countdownCoroutine = StartCoroutine(CountdownRoutine());
    }

    private IEnumerator CountdownRoutine()
    {
        int currentCount = countdownTime;

        while (currentCount > 0)
        {
            countdownText.text = currentCount.ToString();
            yield return new WaitForSeconds(timeBetweenElements);
            currentCount--;
        }

        countdownText.text = countDownEndText;
        onCountdownComplete?.Invoke();

        yield return new WaitForSeconds(timeBetweenElements);
        countdownText.text = "";
    }
}