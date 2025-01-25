using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlimeTrail : MonoBehaviour
{
    private float trailSpeed = 1.0f;
    private bool isStopped = false;
    [SerializeField] private float trailDuration = 10f;
    public void InitialiseTrail(float speed)
    {
        trailSpeed = speed * .09f;
    }
    void FixedUpdate()
    {
        if (!isStopped) transform.localScale += trailSpeed * Time.fixedDeltaTime * Vector3.forward;
    }
    public void StopTrail()
    {
        isStopped = true;
        StartCoroutine(TrailDurationCoroutine());
    }
    IEnumerator TrailDurationCoroutine()
    {
        yield return new WaitForSeconds(trailDuration);
        Destroy(gameObject);
    }
    
}
