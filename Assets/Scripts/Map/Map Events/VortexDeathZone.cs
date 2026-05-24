using System.Collections.Generic;
using UnityEngine;
using FMODUnity;
using System.Collections;

public class VortexDeathZone : MonoBehaviour
{
    private List<PlayerController> playersInRange = new List<PlayerController>();
    private float[] timeInZone = new float[4];
    [SerializeField] private float timeToDeath = 1f;
    [SerializeField] private float startDelay = 25f;
    [SerializeField] private GameObject skull;
    [SerializeField] private EventReference deathEvent;
    private Vortex vortex;
    private SphereCollider sphereCollider;
    private bool isKilling = false;

    private void Start()
    {
        vortex = GetComponentInParent<Vortex>();
        sphereCollider = GetComponentInParent<SphereCollider>();
    }

    private void FixedUpdate()
    {
        if (sphereCollider.enabled)
        {
            for (int i = playersInRange.Count -1; i >= 0; i--)
            {
                int id = playersInRange[i].PlayerID;
                timeInZone[id] += Time.fixedDeltaTime;
                if (timeInZone[id] >= timeToDeath)
                {
                    timeInZone[id] = 0f;
                    RuntimeManager.PlayOneShotAttached(deathEvent, gameObject);
                    playersInRange[i].GetComponent<PlayerStateHandler>().KillPlayer();
                    vortex.RemovePlayer(playersInRange[i]);
                    playersInRange.RemoveAt(i);
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            player.SetDoomed(true);
            playersInRange.Add(player);
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && playersInRange.Contains(other.GetComponent<PlayerController>()))
        {
            PlayerController player = other.GetComponent<PlayerController>();
            player.SetDoomed(false);
            timeInZone[player.PlayerID] = 0f;
            playersInRange.Remove(player);           
        }
    }
    public IEnumerator StartDeathZone()
    {
        yield return new WaitForSeconds(startDelay);
        isKilling = true;
        ToggleDeathZone();
    }
    public void ResetDeathZone()
    {
        playersInRange.Clear();
        isKilling = false;
        ToggleDeathZone();
    }
    private void ToggleDeathZone()
    {
        // Effects n stuff
        sphereCollider.enabled = isKilling;
        skull.SetActive(isKilling);
    }
}