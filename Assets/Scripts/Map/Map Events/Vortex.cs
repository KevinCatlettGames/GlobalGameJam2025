using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Vortex : MapEvent
{
    private List<PlayerController> playersInRange = new List<PlayerController>();
    [Header("Vortex Strength")]
    [SerializeField] private float strength = 1.0f;
    [SerializeField] private float sidewaysStrength = .5f;
    [SerializeField] private AnimationCurve pullForceCurve;
    [SerializeField] private AnimationCurve spinForceCurve;
    [Header("Vortex Growth")]
    [SerializeField] private float resetSpeed = 20f;
    [SerializeField] private float growSpeed = 5.0f;
    [SerializeField] private float minSize = 1f;
    [SerializeField] private float maxSize = 2f;
    [SerializeField] private float startSize = .2f;
    [SerializeField] private float pauseTime = 10f;
    private Vector3 targetScale = Vector3.one;
    private bool isBig = false;
    private float range = 1.0f;
    private float radius = 1.0f;
    private bool reset = false;
    private VortexDeathZone deathZone;
    private void Start()
    {
        deathZone = GetComponentInChildren<VortexDeathZone>();
        radius = GetComponent<SphereCollider>().radius;
        targetScale = Vector3.one *  minSize;
        transform.localScale = Vector3.one * startSize;
    }
    private void FixedUpdate()
    {
        range = radius * transform.localScale.x;
        PlayerController player;
        for (int i = playersInRange.Count - 1; i >= 0; i--)
        {
            player = playersInRange[i];
            Vector3 playerPos = new Vector3(player.transform.position.x, 0, player.transform.position.z);
            // Safety removal when player it too far away
            if (Vector3.Distance(playerPos, transform.position) > range + 3f)
            {
                playersInRange.RemoveAt(i);
                continue;
            }
            Vector3 force = transform.position - playerPos;
            float relativeDistance = (force.magnitude / range);
            force.Normalize();
            relativeDistance = Mathf.Clamp(relativeDistance, 0, 1f);
            Vector3 spin = Vector3.Cross(Vector3.up, force).normalized;
            force *= pullForceCurve.Evaluate(1f - relativeDistance) * strength;
            force += spinForceCurve.Evaluate(1 - relativeDistance) * sidewaysStrength * spin;

            if (GameManager.Instance.PlayingLocal)
                player.GetComponent<PlayerController>().ApplyImpulseLocal(force, 100f * Time.fixedDeltaTime);
            else
                player.GetComponent<PlayerController>().ApplyImpulseServerRpc(force, 100f * Time.fixedDeltaTime);
        }
    }

    private IEnumerator GrowCoroutine()
    {
        strength *= .5f;
        sidewaysStrength *= .5f;
        yield return new WaitForSeconds(pauseTime);
        strength *= 2f;
        sidewaysStrength *= 2f;
        
        while (!reset)
        {
            if (isBig)
                targetScale = Vector3.one * minSize;
            else 
                targetScale = Vector3.one * maxSize;
            while (MathF.Abs(transform.localScale.x - targetScale.x) > .01f)
            {
                transform.localScale =  Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * growSpeed);
                yield return null;
            }
            transform.localScale = targetScale;
            isBig = !isBig;
            yield return new WaitForSeconds(pauseTime);
        }
    }
    private IEnumerator ResetCoroutine()
    {
        targetScale = Vector3.one * startSize;
        while (transform.localScale != targetScale)
        {
            transform.localScale =  Vector3.Lerp(transform.localScale, targetScale, Time.deltaTime * resetSpeed);
            yield return null;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playersInRange.Add(other.GetComponent<PlayerController>());
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && playersInRange.Contains(other.GetComponent<PlayerController>()))
        {
            playersInRange.Remove(other.GetComponent<PlayerController>());
        }
    }
    protected override void StartEvent()
    {
        reset = false;
        StopAllCoroutines();
        StartCoroutine(GrowCoroutine());
    }

    protected override void StopEvent()
    {
        reset = true;
        isBig = false;
        StopAllCoroutines();
        StartCoroutine(ResetCoroutine());
        playersInRange.Clear();
        deathZone?.ResetDeathZone();
    }

    public void RemovePlayer(PlayerController player)
    {
        if (playersInRange.Contains(player))
            playersInRange.Remove(player);
    }
}
