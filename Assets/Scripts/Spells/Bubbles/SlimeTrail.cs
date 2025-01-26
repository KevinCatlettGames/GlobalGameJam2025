using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SlimeTrail : MonoBehaviour
{
    private float trailSpeed = 1.0f;
    private bool isStopped = false;
    [SerializeField] private float trailDuration = 10f;

    private void Start()
    {
        GameManager.Instance.OnGameEnded += RemoveTrail;
    }
    public void InitialiseTrail(float speed)
    {
        trailSpeed = speed * .45f;
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
        RemoveTrail();
    }
    public void RemoveTrail()
    {
        Destroy(gameObject);
    }

    private void OnDestroy()
    {
        GameManager.Instance.OnGameEnded -= RemoveTrail;
    }
}
