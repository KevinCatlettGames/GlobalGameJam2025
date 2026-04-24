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
    private void Start()
    {
        range = GetComponent<SphereCollider>().radius;
    }
    private void FixedUpdate()
    {
        foreach (PlayerController player in playersInRange)
        {
            Vector3 force = transform.position - new Vector3(player.transform.position.x, 0, player.transform.position.z);
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

    }

    protected override void StopEvent()
    {
    }
}
