using Febucci.UI.Core;
using System.Collections.Generic;
using UnityEngine;

public class Vortex : MonoBehaviour 
{
    [SerializeField] private float strength = 1.0f;
    [SerializeField] private float sidewaysStrength = .5f;
    private List<PlayerController> playersInRange = new List<PlayerController>();
    [SerializeField] public AnimationCurve rangeFallOff;
    private float range = 1.0f;
    private void Start()
    {
        bool isMapEventActive = true;
        if (LobbyManager.instance)
            isMapEventActive = LobbyManager.instance.MapSettings[2].PlayWithMapEvent; //Index must be changed once implemented!!!

        if (!isMapEventActive)
        {
            Destroy(gameObject);
            return;
        }
        range = GetComponent<SphereCollider>().radius;
    }
    private void FixedUpdate()
    {
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
}
