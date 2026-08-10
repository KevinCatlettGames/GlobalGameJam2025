
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class EndTutorialZone : MonoBehaviour
{
    private List<PlayerController> playerControllers = new List<PlayerController>();
    [SerializeField] private float increse = .5f;
    [SerializeField] private float decrese = .25f;
    [SerializeField] Slider slider;
    private float progress = 0f;
    private void Update()
    {
        if (playerControllers.Count > 0)
        {
            if (progress >= 1)
            {
                Debug.Log("EXIT TUTORIAL");
                // Exit tutorial
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
