
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EndTutorialZone : MonoBehaviour
{
    private List<PlayerController> playerControllers = new List<PlayerController>();
    [SerializeField] private float increse = .5f;
    [SerializeField] private float decrese = .25f;
    [SerializeField] Slider slider;
    [SerializeField] TutorialMapNetworkInitializer tutorialMapNetworkInitializer;
    private float progress = 0f;
    private bool exitComplete = false;

    private void Update()
    {
        if (exitComplete) return; 

        if (playerControllers.Count > 0)
        {
            if (progress >= 1)
            {
                tutorialMapNetworkInitializer.DespawnTutorialObjects();
                if (MapRotationSystem.Instance && !SteamIntegration.instance || MapRotationSystem.Instance && SteamIntegration.instance && SteamIntegration.instance.IsFullVersion)
                    MapRotationSystem.Instance.CheckForMapSwitch(MapRotationSystem.Instance.MaxRounds);
                else if(SteamIntegration.instance && !SteamIntegration.instance.IsFullVersion)
                    LobbyManager.instance.LoadDemo();
                exitComplete = true;
                //Debug.Log("EXIT TUTORIAL");
            }
            else
            {
                progress += Time.deltaTime * increse;
            }
        }
        else if(progress > 0)
        {
            progress -= Time.deltaTime * decrese;
        }
        slider.value = progress;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController p = other.GetComponent<PlayerController>();
            if (!playerControllers.Contains(p) || p.PlayerID != 5)
            {
                playerControllers.Add(p);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerController p = other.GetComponent<PlayerController>();
            if (playerControllers.Contains(p))
            {
                playerControllers.Remove(p);
            }
        }
    }
}
