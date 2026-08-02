using System;
using UnityEngine;

public class TriggeredText : MonoBehaviour
{
    [SerializeField] private GameObject canvas;
    private bool isActive = false;
    private void OnTriggerEnter(Collider other)
    {
        if (!isActive && other.CompareTag("Player"))
        {
            isActive = true;
            canvas.SetActive(true);
        }
    }
}
