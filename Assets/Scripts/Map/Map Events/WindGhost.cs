using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Splines;

public class WindGhost : MonoBehaviour
{
    private SplineAnimate splineAnimate;
    private GhostExplode explode;

    private void Awake()
    {
        splineAnimate = GetComponent<SplineAnimate>();
        explode = GetComponent<GhostExplode>();
    }

    public void HauntPath(SplineContainer splineContainer)
    {
        splineAnimate.Container = splineContainer;
        splineAnimate.Restart(true);
        explode.Rearm();
        StartCoroutine(hauntPathCoroutine(splineAnimate.Duration));
    }

    private IEnumerator hauntPathCoroutine(float totalTime)
    {
        yield return new WaitForSeconds(totalTime);
        StopGhost();
    }

    public void StopGhost()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);
    }
}
