using System;
using TMPro;
using UnityEngine;

public class Timer : MonoBehaviour
{
    public Action OnTimerComplete;
    private bool isTicking = false;
    private float currentTime = 0;
    
    private TextMeshProUGUI text;
    void Start()
    {
        text = GetComponent<TextMeshProUGUI>();
    }


    void Update()
    {
        if (isTicking && currentTime > 0)
        {
            currentTime -= Time.deltaTime;
            if (currentTime < 0) currentTime = 0;
            int minutes = Mathf.FloorToInt(currentTime / 60);
            int seconds = Mathf.FloorToInt(currentTime % 60);
            text.text = string.Format("{0:00}:{1:00}", minutes, seconds);
            if (currentTime == 0)
            {
                isTicking = false;
                OnTimerComplete?.Invoke();
            }
        }
    }

    public void StartTimer(float time)
    {
        currentTime = time;
        isTicking = true;
    }
}
