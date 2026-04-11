using Febucci.UI.Core;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class Vortex : MapEvent
{
    [SerializeField] private float strength = 1.0f;
    [SerializeField] private float sidewaysStrength = .5f;
    [SerializeField] private float movementRange = 5f;
    [SerializeField] private float speed = 10f;
    [SerializeField] private float resetSpeed = 20f;
    [SerializeField] private float turnRate = 5f;
    private List<PlayerController> playersInRange = new List<PlayerController>();
    [SerializeField] public AnimationCurve rangeFallOff;
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
        if (isRoaming)
        {
            if (Vector3.Distance(transform.position, targetPosition) < .5f)
            {
                Vector2 r = Random.insideUnitCircle;
                r *= movementRange;
                targetPosition = new Vector3(r.x, 0, r.y);
            }
            Vector3 targetVector = targetPosition - transform.position;
            targetVector.y = 0;

            if (targetVector != Vector3.zero)
            {
                targetVector.Normalize();
                direction = Vector3.Lerp(direction, targetVector, Time.fixedDeltaTime * turnRate);
                transform.position = transform.position + direction * speed * Time.deltaTime;
            }
        }
        else if (transform.position != Vector3.zero)
        {
            transform.position = Vector3.Lerp(transform.position, Vector3.zero, Time.fixedDeltaTime * resetSpeed);
        }

        foreach (PlayerController player in playersInRange)
        {
            Vector3 pull = transform.position - player.transform.position;
            float fallOff = (pull.magnitude / range);
            fallOff = Mathf.Clamp(fallOff, 0, 1f);
            fallOff = rangeFallOff.Evaluate(1f - fallOff);
            Vector3 spin = Vector3.Cross(Vector3.up, pull).normalized;
            pull += spin * sidewaysStrength * pull.magnitude;

            if (GameManager.Instance.PlayingLocal)
                player.GetComponent<PlayerController>().ApplyImpulseLocal(pull, strength * fallOff * Time.fixedDeltaTime);
            else
                player.GetComponent<PlayerController>().ApplyImpulseServerRpc(pull, strength * fallOff * Time.fixedDeltaTime);
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
