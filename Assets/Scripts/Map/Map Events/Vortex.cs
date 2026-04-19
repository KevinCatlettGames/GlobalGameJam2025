using Febucci.UI.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.Serialization;

public class Vortex : MapEvent
{
    [SerializeField] private float strength = 1.0f;
    [SerializeField] private float sidewaysStrength = .5f;
    [SerializeField] private float movementRange = 5f;
    [SerializeField] private float speed = 10f;
    [SerializeField] private float resetSpeed = 20f;
    [SerializeField] private float turnRate = 5f;
    private List<PlayerController> playersInRange = new List<PlayerController>();
    [SerializeField] public AnimationCurve pullForceCurve;
    [SerializeField] public AnimationCurve spinForceCurve;
    private float range = 1.0f;
    private Vector3 targetPosition = Vector3.zero;
    private Vector3 direction = Vector3.zero;
    private bool isRoaming = false;
    private void Start()
    {
        range = GetComponent<SphereCollider>().radius;
    }
    private void FixedUpdate()
    {
        // if (isRoaming)
        // {
        //     if (Vector3.Distance(transform.position, targetPosition) < .5f)
        //     {
        //         Vector2 r = Random.insideUnitCircle;
        //         r *= movementRange;
        //         targetPosition = new Vector3(r.x, 0, r.y);
        //     }
        //     Vector3 targetVector = targetPosition - transform.position;
        //     targetVector.y = 0;
        //
        //     if (targetVector != Vector3.zero)
        //     {
        //         targetVector.Normalize();
        //         direction = Vector3.Lerp(direction, targetVector, Time.fixedDeltaTime * turnRate);
        //         transform.position = transform.position + direction * speed * Time.deltaTime;
        //     }
        // }
        // else if (transform.position != Vector3.zero)
        // {
        //     transform.position = Vector3.Lerp(transform.position, Vector3.zero, Time.fixedDeltaTime * resetSpeed);
        // }

        foreach (PlayerController player in playersInRange)
        {
            Vector3 force = transform.position - new Vector3(player.transform.position.x, 0, player.transform.position.z);
            float relativeDistance = (force.magnitude / range);
            force.Normalize();
            relativeDistance = Mathf.Clamp(relativeDistance, 0, 1f);
            Vector3 spin = Vector3.Cross(Vector3.up, force).normalized;
            force *= pullForceCurve.Evaluate(1f - relativeDistance) * strength;
            force *= strength;
            force += spinForceCurve.Evaluate(1 - relativeDistance) * sidewaysStrength * spin;

            if (GameManager.Instance.PlayingLocal)
                player.GetComponent<PlayerController>().ApplyImpulseLocal(force, strength * relativeDistance * Time.fixedDeltaTime);
            else
                player.GetComponent<PlayerController>().ApplyImpulseServerRpc(force, strength * relativeDistance * Time.fixedDeltaTime);
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
        isRoaming = true;
    }

    protected override void StopEvent()
    {
        isRoaming = false;
        targetPosition = Vector3.zero;
    }
}
