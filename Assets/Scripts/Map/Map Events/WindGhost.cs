using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Splines;

public class WindGhost : MonoBehaviour
{
    [SerializeField] private GameObject windBox;
    [SerializeField] private float boxInterval;
    private SplineAnimate splineAnimate;

    private void Awake()
    {
        splineAnimate = GetComponent<SplineAnimate>();
    }

    public void HauntPath(SplineContainer splineContainer)
    {
        splineAnimate.Container = splineContainer;
        splineAnimate.Restart(true);
        StartCoroutine(hauntPathCoroutine(splineAnimate.Duration));
    }

    private IEnumerator hauntPathCoroutine(float totalTime)
    {
        float intervalTime = boxInterval / totalTime;
        float timer = intervalTime;
        while (totalTime > timer)
        {
            yield return null;
            timer += Time.deltaTime;
            if (timer >= intervalTime)
            {
                SpawnWindBox();
                timer -= intervalTime;
                totalTime -= intervalTime;
            }
        }
        gameObject.SetActive(false);
    }

    private void SpawnWindBox()
    {
        Instantiate(windBox, transform.position, transform.rotation);
    }
}
