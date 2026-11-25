using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Splines;

public class LeafGhost : MonoBehaviour
{
    private SplineAnimate splineAnimate;
    private GhostExplode explode;
    [SerializeField] private float reflectTime = 20f;

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

    public void ReflectGhost(Vector3 direction)
    {
        StopAllCoroutines();
        splineAnimate.Pause();
        transform.rotation = Quaternion.LookRotation(direction);
        StartCoroutine(reflectPathCoroutine());
    }
    private IEnumerator reflectPathCoroutine()
    {
        float timer = 0;
        float speed = splineAnimate.MaxSpeed;
        while (timer < reflectTime)
        {
            transform.position = transform.position + transform.forward * speed * Time.deltaTime;
            timer += Time.deltaTime;
            yield return null;
        }
        StopGhost();
    }

    public void StopGhost()
    {
        StopAllCoroutines();
        gameObject.SetActive(false);
    }
}
