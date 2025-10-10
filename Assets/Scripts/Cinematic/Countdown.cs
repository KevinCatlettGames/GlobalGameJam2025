using System.Collections;
using UnityEngine;
using TMPro;
using Unity.Netcode;
using UnityEngine.Events;

public class Countdown : MonoBehaviour
{
    [Header("Countdown Settings")]
    public int countdownTime = 3;
    public TextMeshProUGUI countdownText;

    [Header("Events")]
    public UnityEvent OnCountdownStart;
    public UnityEvent onCountdownComplete;

    private Coroutine countdownCoroutine;

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
            yield return new WaitForSeconds(1f);
            currentCount--;
        }

        countdownText.text = "GO!";
        onCountdownComplete?.Invoke();

        yield return new WaitForSeconds(1f);
        countdownText.text = "";
    }
}